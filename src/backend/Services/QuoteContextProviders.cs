using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Project.Services;

public interface IQuoteWeatherProvider
{
    Task<WeatherSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default);
}

public interface IPublicHolidayProvider
{
    bool IsPortuguesePublicHoliday(DateOnly date);
}

public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.open-meteo.com/";
    public int RequestTimeoutSeconds { get; set; } = 3;
    public int CacheDurationMinutes { get; set; } = 10;
}

public sealed class OpenMeteoWeatherProvider : IQuoteWeatherProvider
{
    private const string PortoWeatherCacheKey = "quote-weather:porto";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly WeatherOptions _options;
    private readonly ILogger<OpenMeteoWeatherProvider> _logger;

    public OpenMeteoWeatherProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<WeatherOptions> options,
        ILogger<OpenMeteoWeatherProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WeatherSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        if (_cache.TryGetValue<WeatherSnapshot>(PortoWeatherCacheKey, out var cachedWeather))
        {
            return cachedWeather;
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                "v1/forecast?latitude=41.1579&longitude=-8.6291&current=temperature_2m,precipitation,cloud_cover,wind_speed_10m",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>(
                cancellationToken: cancellationToken);
            var weather = ToWeatherSnapshot(payload);

            _cache.Set(
                PortoWeatherCacheKey,
                weather,
                TimeSpan.FromMinutes(Math.Clamp(_options.CacheDurationMinutes, 1, 60)));

            return weather;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Current weather request timed out; ML trip duration prediction is unavailable.");
            return null;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Current weather request failed; ML trip duration prediction is unavailable.");
            return null;
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Current weather response was invalid; ML trip duration prediction is unavailable.");
            return null;
        }
        catch (NotSupportedException exception)
        {
            _logger.LogWarning(exception, "Current weather response could not be read; ML trip duration prediction is unavailable.");
            return null;
        }
    }

    private static WeatherSnapshot ToWeatherSnapshot(OpenMeteoResponse? payload)
    {
        var current = payload?.Current
            ?? throw new InvalidOperationException("Weather response does not contain current conditions.");

        var values = new[]
        {
            current.Temperature2m,
            current.Precipitation,
            current.CloudCover,
            current.WindSpeed10m
        };

        if (values.Any(value => !double.IsFinite(value)))
        {
            throw new InvalidOperationException("Weather response contains non-finite values.");
        }

        return new WeatherSnapshot(
            current.Temperature2m,
            current.Precipitation,
            current.CloudCover,
            current.WindSpeed10m);
    }

    private sealed class OpenMeteoResponse
    {
        public CurrentWeather? Current { get; init; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; init; }

        [JsonPropertyName("precipitation")]
        public double Precipitation { get; init; }

        [JsonPropertyName("cloud_cover")]
        public double CloudCover { get; init; }

        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed10m { get; init; }
    }
}

public sealed class PortuguesePublicHolidayProvider : IPublicHolidayProvider
{
    public bool IsPortuguesePublicHoliday(DateOnly date)
    {
        if (IsFixedNationalHoliday(date))
        {
            return true;
        }

        var easterSunday = CalculateEasterSunday(date.Year);
        if (date == easterSunday.AddDays(-2) || date == easterSunday)
        {
            return true;
        }

        return (date.Year <= 2012 || date.Year >= 2016)
            && date == easterSunday.AddDays(60);
    }

    private static bool IsFixedNationalHoliday(DateOnly date)
    {
        if (date.Month == 1 && date.Day == 1
            || date.Month == 4 && date.Day == 25
            || date.Month == 5 && date.Day == 1
            || date.Month == 6 && date.Day == 10
            || date.Month == 8 && date.Day == 15
            || date.Month == 12 && (date.Day == 8 || date.Day == 25))
        {
            return true;
        }

        return (date.Year <= 2012 || date.Year >= 2016)
            && (date.Month, date.Day) is (10, 5) or (11, 1) or (12, 1);
    }

    private static DateOnly CalculateEasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;

        return new DateOnly(year, month, day);
    }
}
