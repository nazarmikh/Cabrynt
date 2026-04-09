using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;
using System.Net.Http.Headers;


namespace backend.IntegrationTests.Me;

public sealed class MeTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MeTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_ReturnsToken_WhenCredentialsAreValid()
    {
        var registerRequest = new
        {
            email = $"it-auth-{Guid.NewGuid():N}@novadrive.test",
            password = "StrongPass123!",
            name = "Test User",
            homeAddress = "Main Street 1",
            preferredPaymentMethod = "Card"
        };

        await _client.PostAsJsonAsync("/api/public/auth/register", registerRequest);

        var loginRequest = new
        {
            email = registerRequest.email,
            password = registerRequest.password
        };

        var responseToken = await _client.PostAsJsonAsync("/api/public/auth/login", loginRequest);
        var loginBody = await responseToken.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var accessToken = loginBody!["accessToken"];

    
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.GetAsync("/api/public/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}