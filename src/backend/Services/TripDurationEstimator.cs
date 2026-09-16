namespace Project.Services;

public interface ITripDurationEstimator
{
    Task<TripDurationEstimate> EstimateAsync(
        RouteRequest route,
        RouteEstimate routeEstimate,
        DateTimeOffset quoteRequestedAt,
        CancellationToken cancellationToken = default);
}

public readonly record struct TripDurationEstimate(
    double DurationMinutes,
    TripDurationEstimateSource Source);

public sealed class TripDurationEstimator : ITripDurationEstimator
{
    private readonly ITripDurationPredictor _predictor;
    private readonly ITripDurationFeatureBuilder _featureBuilder;
    private readonly IQuoteWeatherProvider _weatherProvider;
    private readonly IPublicHolidayProvider _publicHolidayProvider;
    private readonly ILogger<TripDurationEstimator> _logger;

    public TripDurationEstimator(
        ITripDurationPredictor predictor,
        ITripDurationFeatureBuilder featureBuilder,
        IQuoteWeatherProvider weatherProvider,
        IPublicHolidayProvider publicHolidayProvider,
        ILogger<TripDurationEstimator> logger)
    {
        _predictor = predictor;
        _featureBuilder = featureBuilder;
        _weatherProvider = weatherProvider;
        _publicHolidayProvider = publicHolidayProvider;
        _logger = logger;
    }

    public async Task<TripDurationEstimate> EstimateAsync(
        RouteRequest route,
        RouteEstimate routeEstimate,
        DateTimeOffset quoteRequestedAt,
        CancellationToken cancellationToken = default)
    {
        if (routeEstimate.Source != RouteEstimateSource.Osrm)
        {
            return new TripDurationEstimate(
                routeEstimate.DurationMinutes,
                TripDurationEstimateSource.StraightLineFallback);
        }

        if (!_predictor.IsAvailable || !PortoServiceArea.Contains(route))
        {
            return new TripDurationEstimate(routeEstimate.DurationMinutes, TripDurationEstimateSource.Osrm);
        }

        var weather = await _weatherProvider.GetCurrentAsync(cancellationToken);
        if (weather is null)
        {
            return new TripDurationEstimate(routeEstimate.DurationMinutes, TripDurationEstimateSource.Osrm);
        }

        try
        {
            var portoTime = PortoTime.Convert(quoteRequestedAt);
            var features = _featureBuilder.Build(new TripDurationFeatureContext(
                route,
                routeEstimate,
                quoteRequestedAt,
                weather.Value,
                _publicHolidayProvider.IsPortuguesePublicHoliday(DateOnly.FromDateTime(portoTime.DateTime))));
            var residual = _predictor.PredictResidualMinutes(features);

            if (residual is null)
            {
                return new TripDurationEstimate(routeEstimate.DurationMinutes, TripDurationEstimateSource.Osrm);
            }

            return new TripDurationEstimate(
                Math.Max(routeEstimate.DurationMinutes + residual.Value, 0d),
                TripDurationEstimateSource.MachineLearning);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Trip duration model inference failed; using the OSRM duration.");
            return new TripDurationEstimate(routeEstimate.DurationMinutes, TripDurationEstimateSource.Osrm);
        }
    }

}
