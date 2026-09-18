using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Model;

public sealed class ModelInsightsDemoTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ModelInsightsDemoTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClientWithoutCookies();
    }

    [Fact]
    public async Task Estimate_ReturnsPublicRouteEstimate_WithoutAuthentication()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/public/model-insights/estimate",
            CreateRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.True(body!["routeDistance"].GetDecimal() > 0m);
        Assert.True(body["routeDuration"].GetDecimal() > 0m);
        Assert.Equal("StraightLineFallback", body["estimatedTripDurationSource"].GetString());
    }

    [Fact]
    public async Task Estimate_ReturnsTooManyRequests_AfterDemoLimitIsReached()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClientWithoutCookies();

        for (var requestNumber = 0; requestNumber < 10; requestNumber++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/public/model-insights/estimate",
                CreateRequest());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var rateLimitedResponse = await client.PostAsJsonAsync(
            "/api/public/model-insights/estimate",
            CreateRequest());

        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);
    }

    private static object CreateRequest()
    {
        return new
        {
            departureLatitude = 41.149,
            departureLongitude = -8.611,
            destinationLatitude = 41.16,
            destinationLongitude = -8.64,
            preferredServiceTier = "Standard"
        };
    }
}
