using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Project.Services;

namespace backend.UnitTests.Services;

public class OsrmRouteEstimatorTest
{
    [Fact]
    public async Task EstimateAsync_MapsOsrmMetersAndSecondsToKilometersAndMinutes()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(
                "https://osrm.test/route/v1/driving/3.264,50.826;3.717,51.054?overview=false&alternatives=false&steps=false",
                request.RequestUri?.ToString());
            Assert.Contains("Cabrynt/1.0", request.Headers.UserAgent.ToString());

            return JsonResponse(
                """
                {"code":"Ok","routes":[{"distance":12345.6,"duration":789.1}]}
                """);
        });
        var estimator = CreateEstimator(handler, "https://osrm.test/");

        var result = await estimator.EstimateAsync(KortrijkToGhent);

        Assert.Equal(RouteEstimateSource.Osrm, result.Source);
        Assert.InRange(result.DistanceKm, 12.3455, 12.3457);
        Assert.InRange(result.DurationMinutes, 13.1516, 13.1517);
    }

    [Fact]
    public async Task EstimateAsync_UsesStraightLineFallback_WhenOsrmIsNotConfigured()
    {
        var estimator = CreateEstimator(
            new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called")),
            string.Empty);

        var result = await estimator.EstimateAsync(KortrijkToGhent);

        Assert.Equal(RouteEstimateSource.StraightLineFallback, result.Source);
        Assert.True(result.DistanceKm > 0);
        Assert.True(result.DurationMinutes > 0);
    }

    [Fact]
    public async Task EstimateAsync_RejectsRequest_WhenOsrmReportsNoRoute()
    {
        var estimator = CreateEstimator(
            new StubHttpMessageHandler(_ => JsonResponse("""{"code":"NoRoute","routes":[]}""")),
            "https://osrm.test/");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => estimator.EstimateAsync(KortrijkToGhent));

        Assert.Equal(
            "No drivable route exists between the selected departure and destination.",
            exception.Message);
    }

    private static readonly RouteRequest KortrijkToGhent = new(
        DepartureLatitude: 50.826,
        DepartureLongitude: 3.264,
        DestinationLatitude: 51.054,
        DestinationLongitude: 3.717);

    private static OsrmRouteEstimator CreateEstimator(
        HttpMessageHandler handler,
        string osrmBaseUrl)
    {
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Cabrynt/1.0 (+https://github.com/nazarmikh/Cabrynt)");

        return new OsrmRouteEstimator(
            client,
            Options.Create(new RoutingOptions { OsrmBaseUrl = osrmBaseUrl }),
            NullLogger<OsrmRouteEstimator>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
