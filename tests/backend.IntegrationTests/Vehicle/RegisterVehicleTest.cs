using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;
using Project.Models;

namespace backend.IntegrationTests.RegisterVehicle;

public class RegisterVehicleTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public RegisterVehicleTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterVehicle_ReturnsCreated_WhenRequestIsValid()
    {
        var adminLoginRequest = new
        {
            Email = "admin@novadrive.com",
            Password = "AdminPassword123!"
        };

        var adminLoginResponse = await _client.PostAsJsonAsync("/api/public/auth/login", adminLoginRequest);
        var loginBody = await adminLoginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var accessToken = loginBody!["accessToken"];

        // Arrange

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var registerVehicleRequest = new
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020
        };

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken); 
        var response = await _client.PostAsJsonAsync("/api/private/vehicles", registerVehicleRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RegisterVehicle_ReturnedUnauthorized_WhenNotAdmin()
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


        var loginResponse = await _client.PostAsJsonAsync("/api/public/auth/login", loginRequest);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var accessToken = loginBody!["accessToken"];

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var registerVehicleRequest = new
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken); 
        var response = await _client.PostAsJsonAsync("/api/private/vehicles", registerVehicleRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

}