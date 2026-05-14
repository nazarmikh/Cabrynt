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
        var registerRequest = await IntegrationTestData.RegisterPassengerAsync(_client);
        var response = await _client.PostAsJsonAsync("/api/public/auth/login", new
        {
            email = registerRequest.Email,
            password = registerRequest.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.NotNull(body);
        Assert.True(body!.ContainsKey("accessToken"));
        Assert.False(string.IsNullOrWhiteSpace(body["accessToken"]));
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
        var registerRequest = await IntegrationTestData.RegisterPassengerAsync(_client);
        var response = await _client.PostAsJsonAsync("/api/public/auth/login", new
        {
            email = registerRequest.Email,
            password = "WrongPass228"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
