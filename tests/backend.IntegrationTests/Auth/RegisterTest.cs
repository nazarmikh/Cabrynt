using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Auth;

public sealed class RegisterTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RegisterTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClientWithoutCookies();
    }

    [Fact]
    public async Task Register_ReturnsCreated_WhenRequestIsValid()
    {
        var request = IntegrationTestData.CreatePassengerRegistration();
        var response = await _client.PostAsJsonAsync("/api/public/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.True(body!["id"].GetInt32() > 0);
        Assert.Equal(request.Name, body["name"].GetString());
        Assert.Equal(request.HomeAddress, body["homeAddress"].GetString());
        Assert.Equal(request.PreferredPaymentMethod, body["preferredPaymentMethod"].GetString());
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

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var request = IntegrationTestData.CreatePassengerRegistration();

        var firstResponse = await _client.PostAsJsonAsync("/api/public/auth/register", request);
        var duplicateResponse = await _client.PostAsJsonAsync("/api/public/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

}
