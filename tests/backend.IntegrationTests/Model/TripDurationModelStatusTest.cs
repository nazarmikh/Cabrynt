using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Model;

public class TripDurationModelStatusTest
{
    [Fact]
    public async Task GetStatus_ReturnsUnauthorized_WhenNoSessionIsProvided()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClientWithoutCookies();

        var response = await client.GetAsync("/api/private/model-status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_ReturnsDisabled_WhenModelIsDisabled()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClientWithoutCookies();
        await IntegrationTestData.LoginAsAdminAsync(client);

        var response = await client.GetAsync("/api/private/model-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.False(body!["isEnabled"].GetBoolean());
        Assert.False(body["isAvailable"].GetBoolean());
        Assert.Equal("Disabled", body["state"].GetString());
    }

    [Fact]
    public async Task GetStatus_ReturnsReady_WhenConfiguredModelLoads()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sum-23-features.onnx");
        using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["TripDurationModel:Enabled"] = "true",
            ["TripDurationModel:ModelPath"] = modelPath,
            ["TripDurationModel:ExpectedVersion"] = "test-version"
        }, null);
        var client = factory.CreateClientWithoutCookies();
        await IntegrationTestData.LoginAsAdminAsync(client);

        var response = await client.GetAsync("/api/private/model-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.True(body!["isEnabled"].GetBoolean());
        Assert.True(body["isAvailable"].GetBoolean());
        Assert.Equal("Ready", body["state"].GetString());
        Assert.Equal("test-version", body["configuredVersion"].GetString());
    }

    [Fact]
    public async Task GetStatus_ReturnsUnavailable_WhenConfiguredModelCannotLoad()
    {
        using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["TripDurationModel:Enabled"] = "true",
            ["TripDurationModel:ModelPath"] = "missing-model.onnx",
            ["TripDurationModel:ExpectedVersion"] = "test-version"
        }, null);
        var client = factory.CreateClientWithoutCookies();
        await IntegrationTestData.LoginAsAdminAsync(client);

        var response = await client.GetAsync("/api/private/model-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.True(body!["isEnabled"].GetBoolean());
        Assert.False(body["isAvailable"].GetBoolean());
        Assert.Equal("Unavailable", body["state"].GetString());
        Assert.Equal("test-version", body["configuredVersion"].GetString());
    }
}
