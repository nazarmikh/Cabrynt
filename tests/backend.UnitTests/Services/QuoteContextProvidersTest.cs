using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Project.Services;

namespace backend.UnitTests.Services;

public class QuoteContextProvidersTest
{
    [Fact]
    public async Task GetCurrentAsync_MapsAndCachesCurrentOpenMeteoConditions()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(
            """
            {
              "current": {
                "temperature_2m": 17.4,
                "precipitation": 0.2,
                "cloud_cover": 62,
                "wind_speed_10m": 11.8
              }
            }
            """));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = CreateWeatherProvider(handler, cache, enabled: true);

        var first = await provider.GetCurrentAsync();
        var second = await provider.GetCurrentAsync();

        Assert.Equal(new WeatherSnapshot(17.4, 0.2, 62, 11.8), first);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsNoWeather_WhenDisabled()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = CreateWeatherProvider(handler, cache, enabled: false);

        var result = await provider.GetCurrentAsync();

        Assert.Null(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData(2014, 4, 18, true)]
    [InlineData(2014, 4, 20, true)]
    [InlineData(2014, 6, 19, false)]
    [InlineData(2014, 10, 5, false)]
    [InlineData(2016, 5, 26, true)]
    [InlineData(2016, 10, 5, true)]
    [InlineData(2026, 9, 15, false)]
    public void IsPortuguesePublicHoliday_UsesTheApplicableNationalCalendar(
        int year,
        int month,
        int day,
        bool expected)
    {
        IPublicHolidayProvider provider = new PortuguesePublicHolidayProvider();

        var result = provider.IsPortuguesePublicHoliday(new DateOnly(year, month, day));

        Assert.Equal(expected, result);
    }

    private static OpenMeteoWeatherProvider CreateWeatherProvider(
        HttpMessageHandler handler,
        IMemoryCache cache,
        bool enabled)
    {
        return new OpenMeteoWeatherProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://weather.test/") },
            cache,
            Options.Create(new WeatherOptions { Enabled = enabled }),
            NullLogger<OpenMeteoWeatherProvider>.Instance);
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
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responseFactory(request));
        }
    }
}
