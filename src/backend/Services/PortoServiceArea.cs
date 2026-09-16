namespace Project.Services;

public static class PortoServiceArea
{
    public const double MinimumLatitude = 41.09d;
    public const double MaximumLatitude = 41.21d;
    public const double MinimumLongitude = -8.71d;
    public const double MaximumLongitude = -8.53d;

    public static bool Contains(RouteRequest route)
    {
        return Contains(route.DepartureLatitude, route.DepartureLongitude)
            && Contains(route.DestinationLatitude, route.DestinationLongitude);
    }

    public static bool Contains(double latitude, double longitude)
    {
        return latitude is >= MinimumLatitude and <= MaximumLatitude
            && longitude is >= MinimumLongitude and <= MaximumLongitude;
    }
}
