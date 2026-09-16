using System.Net;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Health;

public sealed class HealthEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClientWithoutCookies();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ReturnsOk_WhenApplicationAndDatabaseAreAvailable(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
