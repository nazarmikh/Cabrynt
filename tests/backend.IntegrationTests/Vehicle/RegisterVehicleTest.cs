using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;

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
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var result = await IntegrationTestData.RegisterVehicleAsync(_client);

        Assert.True(result.VehicleId > 0);
        Assert.Equal(result.Request.VIN, result.VIN);
        Assert.Equal(result.Request.LicencePlate, result.LicencePlate);
        Assert.Equal(result.Request.Model, result.Model);
        Assert.Equal(result.Request.Year, result.Year);
        Assert.Equal("Standard", result.VehicleType);
        Assert.Equal("Active", result.VehicleStatus);
    }

    [Fact]
    public async Task RegisterVehicle_ReturnedUnauthorized_WhenNotAdmin()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client, $"it-register-vehicle-{Guid.NewGuid():N}@cabrynt.test");
        var passengerSession = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerSession);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var response = await _client.PostAsJsonAsync("/api/private/vehicles", new
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = $"it-register-vehicle-{Guid.NewGuid():N}@cabrynt.test",
            SystemPassword = IntegrationTestData.DefaultPassword
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegisterVehicle_ReturnsConflict_WhenVinAlreadyExists()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var existingVin = $"1HGCM82633A{suffix}";

        var firstResponse = await _client.PostAsJsonAsync("/api/private/vehicles", new
        {
            VIN = existingVin,
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = $"it-register-vehicle-{Guid.NewGuid():N}@cabrynt.test",
            SystemPassword = IntegrationTestData.DefaultPassword
        });

        var duplicateResponse = await _client.PostAsJsonAsync("/api/private/vehicles", new
        {
            VIN = existingVin,
            LicencePlate = $"DUPL{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = $"it-register-vehicle-{Guid.NewGuid():N}@cabrynt.test",
            SystemPassword = IntegrationTestData.DefaultPassword
        });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task RegisterVehicle_ReturnsConflict_WhenLicencePlateAlreadyExists()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var existingLicencePlate = $"TEST{suffix}";

        var firstResponse = await _client.PostAsJsonAsync("/api/private/vehicles", new
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = existingLicencePlate,
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = $"it-register-vehicle-{Guid.NewGuid():N}@cabrynt.test",
            SystemPassword = IntegrationTestData.DefaultPassword
        });

        var duplicateResponse = await _client.PostAsJsonAsync("/api/private/vehicles", new
        {
            VIN = $"1HGCM82633B{suffix}",
            LicencePlate = existingLicencePlate,
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = $"it-register-vehicle-{Guid.NewGuid():N}@cabrynt.test",
            SystemPassword = IntegrationTestData.DefaultPassword
        });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

}
