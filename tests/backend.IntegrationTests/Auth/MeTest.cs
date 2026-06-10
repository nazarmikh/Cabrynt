using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;
using System.Text.Json;


namespace backend.IntegrationTests.Me;

public sealed class MeTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MeTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_ReturnsPassengerProfile_WhenCredentialsAreValid()
    {
        var registerRequest = await IntegrationTestData.RegisterPassengerAsync(_client);

        await IntegrationTestData.LoginAsync(
            _client,
            registerRequest.Email,
            registerRequest.Password);

        var response = await _client.GetAsync("/api/public/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.Equal(registerRequest.Email, body!["email"].GetString());
        Assert.Equal(registerRequest.Name, body["name"].GetString());
        Assert.Equal(registerRequest.HomeAddress, body["homeAddress"].GetString());
        Assert.Equal(registerRequest.PreferredPaymentMethod, body["preferredPaymentMethod"].GetString());
    }

    [Fact]
    public async Task Me_ReturnUnauthorized_WhenNoToken()
    {
        var response = await _client.GetAsync("/api/public/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
