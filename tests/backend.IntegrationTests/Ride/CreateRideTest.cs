using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
namespace backend.IntegrationTests.CreateRide;

public class CreateRideTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public CreateRideTest (CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRide_ReturnsCreated_WhenRequestIsValid()
    {
        var adminLoginRequest = new
        {
            email = "admin@novadrive.com",
            password = "AdminPassword123!"
        };

        var adminLoginResponse = await _client.PostAsJsonAsync("/api/public/auth/login", adminLoginRequest);
        adminLoginResponse.EnsureSuccessStatusCode();

        var loginResponseJson = await adminLoginResponse.Content.ReadFromJsonAsync<Dictionary<string,string>>();
        var adminToken = loginResponseJson!["accessToken"];
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken); 

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        string generatedEmail = $"it-create-ride-{Guid.NewGuid():N}@novadrive.test"; 

        var createVehicleRequest = new
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = generatedEmail,
            SystemPassword = "StrongPass123!"
        };

        var createVehicleResponse = await _client.PostAsJsonAsync("/api/private/vehicles", createVehicleRequest);
        createVehicleResponse.EnsureSuccessStatusCode();
        var vehicleResponseJson = await createVehicleResponse.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        var vehicleId = vehicleResponseJson!["vehicleId"].GetInt32();

        var vehicleLogInRequest = new
        {
            email = generatedEmail,
            password = "StrongPass123!"
        };

        var vehicleLogInResponse = await _client.PostAsJsonAsync("/api/public/auth/login", vehicleLogInRequest);
        vehicleLogInResponse.EnsureSuccessStatusCode();
        var vehicleTokenJson = await vehicleLogInResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        string vehicleToken = vehicleTokenJson!["accessToken"];
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vehicleToken); 

        var random = new Random();

        var createTelemetryRequest = new
        {
            Latitude = random.NextDouble() * 180 - 90,
            Longitude = random.NextDouble() * 360 - 180,
            CurrentSpeed = random.Next(0, 120),
            RemainingBatteryPercentage = random.Next(0, 101),
            HardwareTemperature = random.Next(5,70),
            VehicleId = vehicleId
        };

        var createTelemetryResponse = await _client.PostAsJsonAsync("/api/private/telemetry", createTelemetryRequest);
        createTelemetryResponse.EnsureSuccessStatusCode();

        generatedEmail = $"it-auth-{Guid.NewGuid():N}@novadrive.test";

        var createUserRequest = new
        {
            email = generatedEmail,
            password = "StrongPass123!",
            name = "Test User",
            homeAddress = "Main Street 1",
            preferredPaymentMethod = "Card"
        };

        var createUserResponse = await _client.PostAsJsonAsync("/api/public/auth/register", createUserRequest);
        createUserResponse.EnsureSuccessStatusCode();

        var userLoginRequest = new
        {
            email = generatedEmail,
            password = "StrongPass123!"
        };

        var userLoginResponse = await _client.PostAsJsonAsync("/api/public/auth/login", userLoginRequest);
        userLoginResponse.EnsureSuccessStatusCode();

        var userLoginResponseJson = await userLoginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        string userToken = userLoginResponseJson!["accessToken"];
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken); 

        var createRideRequest = new
        {
            DepartureLocation = "Kortrijk",
            DestinationLocation = "Ghent",
            DepartureLatitude = random.NextDouble() * 180 - 90,
            DepartureLongitude = random.NextDouble() * 360 - 180,
        };

        var createRideResponse = await _client.PostAsJsonAsync("/api/public/rides", createRideRequest);
        Assert.Equal(HttpStatusCode.Created, createRideResponse.StatusCode);

        var rideBody = await createRideResponse.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.True(rideBody!["vehicleId"].ValueKind != JsonValueKind.Null);
        Assert.Equal("Requested", rideBody["rideStatus"].GetString());

    }
}
