using Microsoft.Extensions.Logging.Abstractions;
using Project.Enums;
using Project.Services;

namespace backend.UnitTests.Services;

public class TripDurationEstimatorTest
{
    [Fact]
    public async Task EstimateAsync_UsesModelCorrection_WhenAllQuoteTimeContextIsAvailable()
    {
        var predictor = new StubPredictor(isAvailable: true, residualMinutes: -2f);
        var estimator = CreateEstimator(predictor, new StubWeatherProvider(new WeatherSnapshot(18, 0, 50, 12)));

        var result = await estimator.EstimateAsync(PortoRoute, OsrmEstimate, QuoteTime);

        Assert.Equal(TripDurationEstimateSource.MachineLearning, result.Source);
        Assert.Equal(10d, result.DurationMinutes);
        Assert.Equal(1, predictor.PredictionCount);
    }

    [Fact]
    public async Task EstimateAsync_UsesOsrm_WhenModelIsUnavailable()
    {
        var predictor = new StubPredictor(isAvailable: false, residualMinutes: 3f);
        var estimator = CreateEstimator(predictor, new StubWeatherProvider(new WeatherSnapshot(18, 0, 50, 12)));

        var result = await estimator.EstimateAsync(PortoRoute, OsrmEstimate, QuoteTime);

        Assert.Equal(TripDurationEstimateSource.Osrm, result.Source);
        Assert.Equal(12d, result.DurationMinutes);
        Assert.Equal(0, predictor.PredictionCount);
    }

    [Fact]
    public async Task EstimateAsync_UsesOsrm_WhenWeatherIsUnavailable()
    {
        var predictor = new StubPredictor(isAvailable: true, residualMinutes: 3f);
        var estimator = CreateEstimator(predictor, new StubWeatherProvider(null));

        var result = await estimator.EstimateAsync(PortoRoute, OsrmEstimate, QuoteTime);

        Assert.Equal(TripDurationEstimateSource.Osrm, result.Source);
        Assert.Equal(12d, result.DurationMinutes);
        Assert.Equal(0, predictor.PredictionCount);
    }

    [Fact]
    public async Task EstimateAsync_UsesStraightLineFallback_WhenOsrmIsUnavailable()
    {
        var predictor = new StubPredictor(isAvailable: true, residualMinutes: 3f);
        var estimator = CreateEstimator(predictor, new StubWeatherProvider(new WeatherSnapshot(18, 0, 50, 12)));
        var fallbackRoute = new RouteEstimate(3, 5, RouteEstimateSource.StraightLineFallback);

        var result = await estimator.EstimateAsync(PortoRoute, fallbackRoute, QuoteTime);

        Assert.Equal(TripDurationEstimateSource.StraightLineFallback, result.Source);
        Assert.Equal(5d, result.DurationMinutes);
        Assert.Equal(0, predictor.PredictionCount);
    }

    [Fact]
    public async Task EstimateAsync_UsesOsrmOutsideThePortoModelArea()
    {
        var predictor = new StubPredictor(isAvailable: true, residualMinutes: 3f);
        var estimator = CreateEstimator(predictor, new StubWeatherProvider(new WeatherSnapshot(18, 0, 50, 12)));
        var kortrijkRoute = new RouteRequest(50.826, 3.264, 51.054, 3.717);

        var result = await estimator.EstimateAsync(kortrijkRoute, OsrmEstimate, QuoteTime);

        Assert.Equal(TripDurationEstimateSource.Osrm, result.Source);
        Assert.Equal(12d, result.DurationMinutes);
        Assert.Equal(0, predictor.PredictionCount);
    }

    private static readonly RouteRequest PortoRoute = new(41.1579, -8.6291, 41.17, -8.65);
    private static readonly RouteEstimate OsrmEstimate = new(4.2, 12, RouteEstimateSource.Osrm);
    private static readonly DateTimeOffset QuoteTime = new(2024, 1, 1, 14, 30, 0, TimeSpan.Zero);

    private static TripDurationEstimator CreateEstimator(
        ITripDurationPredictor predictor,
        IQuoteWeatherProvider weatherProvider)
    {
        return new TripDurationEstimator(
            predictor,
            new TripDurationFeatureBuilder(),
            weatherProvider,
            new PortuguesePublicHolidayProvider(),
            NullLogger<TripDurationEstimator>.Instance);
    }

    private sealed class StubPredictor(bool isAvailable, float? residualMinutes) : ITripDurationPredictor
    {
        public bool IsAvailable { get; } = isAvailable;
        public int PredictionCount { get; private set; }

        public float? PredictResidualMinutes(IReadOnlyList<float> features)
        {
            PredictionCount++;
            return residualMinutes;
        }
    }

    private sealed class StubWeatherProvider(WeatherSnapshot? weather) : IQuoteWeatherProvider
    {
        public Task<WeatherSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(weather);
        }
    }
}
