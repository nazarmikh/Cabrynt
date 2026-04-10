using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
using Project.DTOs;
using Project.Models;

namespace backend.IntegrationTests.Telemetry;

public class TelemetryTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public TelemetryTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddTelemetry_ReturnsCreated_WhenRequestIsValid()
    {
        var adminLoginRequest = new
        {
            Email = "admin@novadrive.com",
            Password = "AdminPassword123!"
        };

        var adminLoginResponse = await _client.PostAsJsonAsync("/api/public/auth/login", adminLoginRequest);
        var loginBody = await adminLoginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var accessToken = loginBody!["accessToken"];

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var registerVehicleRequest = new
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = $"it-register-vehicle-{Guid.NewGuid():N}@novadrive.test",
            SystemPassword = "StrongPass123!"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken); 
        var registerVehicleResponse = await _client.PostAsJsonAsync("/api/private/vehicles", registerVehicleRequest);
        Assert.Equal(HttpStatusCode.Created, registerVehicleResponse.StatusCode);

        var vehicleBody = await registerVehicleResponse.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        var vehicleId = vehicleBody!["vehicleId"].GetInt32();


        var loginVehicleRequest = new
        {
            Email = registerVehicleRequest.SystemEmail,
            Password = registerVehicleRequest.SystemPassword
        };

        var loginVehicleResponse = await _client.PostAsJsonAsync("/api/public/auth/login", loginVehicleRequest);
        var loginVehicleBody = await loginVehicleResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var vehicleToken = loginVehicleBody!["accessToken"];


        Random random = new Random();
        var addTelementryRequest = new
        {
            Latitude = random.NextDouble() * 180 - 90,
            Longitude = random.NextDouble() * 360 - 180,
            CurrentSpeed = random.Next(0, 120),
            RemainingBatteryPercentage = random.Next(0, 101),
            HardwareTemperature = random.Next(5,70),
            VehicleId = vehicleId
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vehicleToken); 

        var addTelementryResponse = await _client.PostAsJsonAsync("/api/private/telemetry", addTelementryRequest);

        Assert.Equal(HttpStatusCode.Created, addTelementryResponse.StatusCode);
    }

}