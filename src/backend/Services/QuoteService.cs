using Microsoft.Extensions.Options;

namespace Project.Services;

public interface IQuoteService
{
    Task<QuoteCalculation> CalculateAsync(
        RideRequestDto rideRequest,
        DateTimeOffset quoteRequestedAt,
        CancellationToken cancellationToken = default);
}

public sealed record QuoteCalculation(
    PriceQuoteBreakdown Breakdown,
    decimal EstimatedTripDuration,
    TripDurationEstimateSource EstimatedTripDurationSource,
    string? TripDurationModelVersion,
    RouteEstimateSource RouteEstimateSource);

public sealed class QuoteService : IQuoteService
{
    private readonly IPriceService _priceService;
    private readonly IRouteEstimator _routeEstimator;
    private readonly ITripDurationEstimator _tripDurationEstimator;
    private readonly TripDurationModelOptions _tripDurationModelOptions;

    public QuoteService(
        IPriceService priceService,
        IRouteEstimator routeEstimator,
        ITripDurationEstimator tripDurationEstimator,
        IOptions<TripDurationModelOptions> tripDurationModelOptions)
    {
        _priceService = priceService;
        _routeEstimator = routeEstimator;
        _tripDurationEstimator = tripDurationEstimator;
        _tripDurationModelOptions = tripDurationModelOptions.Value;
    }

    public async Task<QuoteCalculation> CalculateAsync(
        RideRequestDto rideRequest,
        DateTimeOffset quoteRequestedAt,
        CancellationToken cancellationToken = default)
    {
        var route = CreateRouteRequest(rideRequest);
        var routeEstimate = await _routeEstimator.EstimateAsync(route, cancellationToken);
        var distance = Math.Round((decimal)routeEstimate.DistanceKm, 2);
        var duration = Math.Round((decimal)routeEstimate.DurationMinutes, 2);
        var tripDurationEstimate = await _tripDurationEstimator.EstimateAsync(
            route,
            routeEstimate,
            quoteRequestedAt,
            cancellationToken);
        var breakdown = _priceService.GetEstimatedBreakdown(
            distance,
            duration,
            rideRequest.PreferredVehicleType,
            quoteRequestedAt.UtcDateTime);

        return new QuoteCalculation(
            breakdown,
            Math.Round((decimal)tripDurationEstimate.DurationMinutes, 2),
            tripDurationEstimate.Source,
            tripDurationEstimate.Source == TripDurationEstimateSource.MachineLearning
                ? _tripDurationModelOptions.ExpectedVersion
                : null,
            routeEstimate.Source);
    }

    private static RouteRequest CreateRouteRequest(RideRequestDto rideRequest)
    {
        return new RouteRequest(
            rideRequest.DepartureLatitude,
            rideRequest.DepartureLongitude,
            rideRequest.DestinationLatitude,
            rideRequest.DestinationLongitude);
    }
}
