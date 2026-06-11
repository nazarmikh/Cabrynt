using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Maintenance;

public class GetMaintenanceTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetMaintenanceTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClientWithoutCookies();
    }

    [Fact]
    public async Task GetMaintenances_ReturnsOnlyRecordsForRequestedVehicle()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var firstVehicle = await IntegrationTestData.RegisterVehicleAsync(_client);
        var secondVehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        await CreateMaintenanceAsync(firstVehicle.VehicleId, "Brake check", "Alex Turner", 100m, 12000);
        await CreateMaintenanceAsync(firstVehicle.VehicleId, "Battery diagnostics", "Sam Reed", 80m, 15000);
        await CreateMaintenanceAsync(secondVehicle.VehicleId, "Tyre replacement", "Jordan Price", 220m, 20000);

        var response = await _client.GetAsync($"/api/private/vehicles/{firstVehicle.VehicleId}/maintenances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);

        var descriptions = body!["maintenances"]
            .EnumerateArray()
            .Select(m => m.GetProperty("description").GetString())
            .ToList();

        Assert.Equal(2, descriptions.Count);
        Assert.Contains("Brake check", descriptions);
        Assert.Contains("Battery diagnostics", descriptions);
        Assert.DoesNotContain("Tyre replacement", descriptions);
    }

    [Fact]
    public async Task GetMaintenances_ReturnsEmptyList_WhenVehicleHasNoRecords()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var response = await _client.GetAsync($"/api/private/vehicles/{vehicle.VehicleId}/maintenances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.Empty(body!["maintenances"].EnumerateArray());
    }

    [Fact]
    public async Task GetMaintenances_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.GetAsync("/api/private/vehicles/1/maintenances");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMaintenances_ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.GetAsync("/api/private/vehicles/1/maintenances");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMaintenances_ReturnsNotFound_WhenVehicleDoesNotExist()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.GetAsync("/api/private/vehicles/999999/maintenances");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaintenanceById_ReturnsRecord_WhenItBelongsToVehicle()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);
        var maintenanceId = await CreateMaintenanceAsync(vehicle.VehicleId, "Sensor recalibration", "Alex Turner", 150m, 18000);

        var response = await _client.GetAsync($"/api/private/vehicles/{vehicle.VehicleId}/maintenances/{maintenanceId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.Equal(maintenanceId, body!["id"].GetInt32());
        Assert.Equal(vehicle.VehicleId, body["vehicleId"].GetInt32());
        Assert.Equal("Sensor recalibration", body["description"].GetString());
    }

    [Fact]
    public async Task GetMaintenanceById_ReturnsNotFound_WhenMaintenanceDoesNotBelongToVehicle()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var firstVehicle = await IntegrationTestData.RegisterVehicleAsync(_client);
        var secondVehicle = await IntegrationTestData.RegisterVehicleAsync(_client);
        var maintenanceId = await CreateMaintenanceAsync(firstVehicle.VehicleId, "Private maintenance", "Alex Turner", 90m, 14000);

        var response = await _client.GetAsync($"/api/private/vehicles/{secondVehicle.VehicleId}/maintenances/{maintenanceId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaintenanceById_ReturnsNotFound_WhenMaintenanceDoesNotExist()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var response = await _client.GetAsync($"/api/private/vehicles/{vehicle.VehicleId}/maintenances/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMaintenanceById_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.GetAsync("/api/private/vehicles/1/maintenances/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMaintenanceById_ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.GetAsync("/api/private/vehicles/1/maintenances/1");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> CreateMaintenanceAsync(int vehicleId, string description, string technicianName, decimal cost, int nextInspectionMileage)
    {
        var response = await _client.PostAsJsonAsync($"/api/private/vehicles/{vehicleId}/maintenances", new
        {
            serviceDate = DateTime.UtcNow.Date,
            description,
            technicianName,
            cost,
            nextInspectionMileage
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);

        return body!["id"].GetInt32();
    }
}
