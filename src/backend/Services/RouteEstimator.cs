using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Project.Services;

public interface IRouteEstimator
{
    Task<RouteEstimate> EstimateAsync(
        RouteRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class RoutingOptions
{
    public const string SectionName = "Routing";

    public string OsrmBaseUrl { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "Cabrynt/1.0 (+https://github.com/nazarmikh/Cabrynt)";
    public int RequestTimeoutSeconds { get; set; } = 3;
}

public readonly record struct RouteRequest(
    double DepartureLatitude,
    double DepartureLongitude,
    double DestinationLatitude,
    double DestinationLongitude);

public readonly record struct RouteEstimate(
    double DistanceKm,
    double DurationMinutes,
    RouteEstimateSource Source);

public enum RouteEstimateSource
{
    Osrm,
    StraightLineFallback
}

public sealed class OsrmRouteEstimator : IRouteEstimator
{
    private const decimal FallbackAverageSpeedKmPerHour = 40m;

    private readonly HttpClient _httpClient;
    private readonly RoutingOptions _options;
    private readonly ILogger<OsrmRouteEstimator> _logger;

    public OsrmRouteEstimator(
        HttpClient httpClient,
        IOptions<RoutingOptions> options,
        ILogger<OsrmRouteEstimator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RouteEstimate> EstimateAsync(
        RouteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OsrmBaseUrl))
        {
            _logger.LogDebug("OSRM is not configured; using the straight-line route fallback.");
            return CalculateStraightLineFallback(request);
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                BuildRouteUri(request),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"OSRM returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            var payload = await response.Content.ReadFromJsonAsync<OsrmRouteResponse>(
                cancellationToken: cancellationToken);

            if (string.Equals(payload?.Code, "NoRoute", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "No drivable route exists between the selected departure and destination.");
            }

            var route = payload?.Routes?.FirstOrDefault();
            if (!string.Equals(payload?.Code, "Ok", StringComparison.Ordinal)
                || route is null
                || !double.IsFinite(route.Distance)
                || !double.IsFinite(route.Duration)
                || route.Distance < 0
                || route.Duration < 0)
            {
                throw new HttpRequestException("OSRM returned an invalid route response.");
            }

            return new RouteEstimate(
                DistanceKm: route.Distance / 1_000d,
                DurationMinutes: route.Duration / 60d,
                Source: RouteEstimateSource.Osrm);
        }
        catch (InvalidOperationException) when (!cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("OSRM timed out; using the straight-line route fallback.");
            return CalculateStraightLineFallback(request);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "OSRM was unavailable; using the straight-line route fallback.");
            return CalculateStraightLineFallback(request);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "OSRM returned invalid JSON; using the straight-line route fallback.");
            return CalculateStraightLineFallback(request);
        }
    }

    private Uri BuildRouteUri(RouteRequest request)
    {
        var baseUri = new Uri(
            _options.OsrmBaseUrl.TrimEnd('/') + "/",
            UriKind.Absolute);
        var coordinates = string.Join(
            ";",
            string.Create(
                CultureInfo.InvariantCulture,
                $"{request.DepartureLongitude},{request.DepartureLatitude}"),
            string.Create(
                CultureInfo.InvariantCulture,
                $"{request.DestinationLongitude},{request.DestinationLatitude}"));

        return new Uri(
            baseUri,
            $"route/v1/driving/{coordinates}?overview=false&alternatives=false&steps=false");
    }

    private static RouteEstimate CalculateStraightLineFallback(RouteRequest request)
    {
        const double earthRadiusKm = 6_371d;

        static double ToRadians(double angle) => Math.PI * angle / 180d;

        var latitudeDifference = ToRadians(request.DestinationLatitude - request.DepartureLatitude);
        var longitudeDifference = ToRadians(request.DestinationLongitude - request.DepartureLongitude);
        var departureLatitude = ToRadians(request.DepartureLatitude);
        var destinationLatitude = ToRadians(request.DestinationLatitude);

        var haversine =
            Math.Sin(latitudeDifference / 2d) * Math.Sin(latitudeDifference / 2d) +
            Math.Cos(departureLatitude) * Math.Cos(destinationLatitude) *
            Math.Sin(longitudeDifference / 2d) * Math.Sin(longitudeDifference / 2d);
        var distanceKm = 2d * earthRadiusKm * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1d - haversine));
        var roundedDistanceKm = Math.Round((decimal)distanceKm, 2);
        var durationMinutes = Math.Round(
            roundedDistanceKm / FallbackAverageSpeedKmPerHour * 60m,
            2);

        return new RouteEstimate(
            DistanceKm: (double)roundedDistanceKm,
            DurationMinutes: (double)durationMinutes,
            Source: RouteEstimateSource.StraightLineFallback);
    }

    private sealed class OsrmRouteResponse
    {
        public string? Code { get; init; }
        public IReadOnlyList<OsrmRoute>? Routes { get; init; }
    }

    private sealed class OsrmRoute
    {
        public double Distance { get; init; }
        public double Duration { get; init; }
    }
}
