using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Auth;

public sealed class RegisterTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RegisterTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsCreated_WhenRequestIsValid()
    {
        var request = new
        {
            email = $"it-auth-{Guid.NewGuid():N}@novadrive.test",
            password = "StrongPass123!",
            name = "Test User",
            homeAddress = "Main Street 1",
            preferredPaymentMethod = "Card"
        };

        var response = await _client.PostAsJsonAsync("/api/public/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenRequestIsInvalid()
    {
        var request = new
        {
            email = "invalid-email",
            password = "weak",
            name = "Test User",
            homeAddress = "Main Street 1",
            preferredPaymentMethod = "Card"
        };

        var response = await _client.PostAsJsonAsync("/api/public/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    }

}
