namespace Project.Services;

public interface ITripDurationFeatureBuilder
{
    float[] Build(TripDurationFeatureContext context);
}

public readonly record struct WeatherSnapshot(
    double TemperatureC,
    double PrecipitationMm,
    double CloudCoverPercent,
    double WindSpeedKmh);

public readonly record struct TripDurationFeatureContext(
    RouteRequest Route,
    RouteEstimate RouteEstimate,
    DateTimeOffset QuoteRequestedAt,
    WeatherSnapshot Weather,
    bool IsPublicHoliday);

public sealed class TripDurationFeatureBuilder : ITripDurationFeatureBuilder
{
    private const double EarthRadiusKm = 6_371d;
    private static readonly TimeZoneInfo PortoTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");

    public float[] Build(TripDurationFeatureContext context)
    {
        ValidateContext(context);

        var localTime = TimeZoneInfo.ConvertTime(context.QuoteRequestedAt, PortoTimeZone);
        var hour = localTime.Hour;
        var weekday = ((int)localTime.DayOfWeek + 6) % 7;
        var month = localTime.Month;
        var straightLineKm = CalculateHaversineKm(context.Route);
        var weather = context.Weather;

        return
        [
            (float)context.Route.DepartureLongitude,
            (float)context.Route.DepartureLatitude,
            (float)context.Route.DestinationLongitude,
            (float)context.Route.DestinationLatitude,
            (float)straightLineKm,
            hour,
            weekday,
            month,
            weekday >= 5 ? 1 : 0,
            context.IsPublicHoliday ? 1 : 0,
            (float)Math.Sin(2d * Math.PI * hour / 24d),
            (float)Math.Cos(2d * Math.PI * hour / 24d),
            (float)Math.Sin(2d * Math.PI * weekday / 7d),
            (float)Math.Cos(2d * Math.PI * weekday / 7d),
            (float)Math.Sin(2d * Math.PI * month / 12d),
            (float)Math.Cos(2d * Math.PI * month / 12d),
            (float)weather.TemperatureC,
            (float)weather.PrecipitationMm,
            (float)weather.CloudCoverPercent,
            (float)weather.WindSpeedKmh,
            weather.PrecipitationMm > 0d ? 1 : 0,
            (float)context.RouteEstimate.DistanceKm,
            (float)context.RouteEstimate.DurationMinutes
        ];
    }

    private static void ValidateContext(TripDurationFeatureContext context)
    {
        if (context.RouteEstimate.Source != RouteEstimateSource.Osrm)
        {
            throw new InvalidOperationException(
                "Trip duration features require an OSRM route estimate.");
        }

        var values = new[]
        {
            context.Route.DepartureLatitude,
            context.Route.DepartureLongitude,
            context.Route.DestinationLatitude,
            context.Route.DestinationLongitude,
            context.RouteEstimate.DistanceKm,
            context.RouteEstimate.DurationMinutes,
            context.Weather.TemperatureC,
            context.Weather.PrecipitationMm,
            context.Weather.CloudCoverPercent,
            context.Weather.WindSpeedKmh
        };

        if (values.Any(value => !double.IsFinite(value)))
        {
            throw new ArgumentException(
                "Trip duration feature values must be finite.",
                nameof(context));
        }

        if (context.RouteEstimate.DistanceKm < 0d || context.RouteEstimate.DurationMinutes < 0d)
        {
            throw new ArgumentException(
                "Trip duration route estimates cannot be negative.",
                nameof(context));
        }
    }

    private static double CalculateHaversineKm(RouteRequest route)
    {
        var departureLatitude = ToRadians(route.DepartureLatitude);
        var destinationLatitude = ToRadians(route.DestinationLatitude);
        var latitudeDifference = destinationLatitude - departureLatitude;
        var longitudeDifference = ToRadians(route.DestinationLongitude - route.DepartureLongitude);
        var haversine =
            Math.Pow(Math.Sin(latitudeDifference / 2d), 2d) +
            Math.Cos(departureLatitude) * Math.Cos(destinationLatitude) *
            Math.Pow(Math.Sin(longitudeDifference / 2d), 2d);

        return 2d * EarthRadiusKm * Math.Asin(Math.Sqrt(double.Clamp(haversine, 0d, 1d)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
