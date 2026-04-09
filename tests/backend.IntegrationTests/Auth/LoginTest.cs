using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Auth;

public sealed class LoginTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
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

        var request = new
        {
            email = registerRequest.email,
            password = registerRequest.password
        };

        var response = await _client.PostAsJsonAsync("/api/public/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        var loginRequest = new
        {
            email = "invalid@example.com",
            password = "InvalidPass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/public/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
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
            password = "WrongPass228"
        };

        var response = await _client.PostAsJsonAsync("/api/public/auth/login", loginRequest);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
