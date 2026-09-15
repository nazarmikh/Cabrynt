namespace Project.Services;

public static class PortoTime
{
    private static readonly TimeZoneInfo TimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");

    public static DateTimeOffset Convert(DateTimeOffset timestamp)
    {
        return TimeZoneInfo.ConvertTime(timestamp, TimeZone);
    }
}
