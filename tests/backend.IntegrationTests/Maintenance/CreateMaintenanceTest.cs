using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Data;

namespace backend.IntegrationTests.Maintenance;

public class CreateMaintenanceTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CreateMaintenanceTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithoutCookies();
    }

    [Fact]
    public async Task CreateMaintenance_ReturnsCreated_AndPersistsRecord_WhenRequestIsValid()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var request = new
        {
            serviceDate = DateTime.UtcNow.Date,
            description = "Annual inspection and brake pad replacement.",
            technicianName = "Alex Turner",
            cost = 249.99m,
            nextInspectionMileage = 45000
        };

        var response = await _client.PostAsJsonAsync($"/api/private/vehicles/{vehicle.VehicleId}/maintenances", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.True(body!["id"].GetInt32() > 0);
        Assert.Equal(request.description, body["description"].GetString());
        Assert.Equal(request.technicianName, body["technicianName"].GetString());
        Assert.Equal(request.cost, body["cost"].GetDecimal());
        Assert.Equal(request.nextInspectionMileage, body["nextInspectionMileage"].GetInt32());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var createdMaintenance = await dbContext.Maintenances
            .Include(m => m.Vehicle)
            .SingleAsync(m => m.Id == body["id"].GetInt32());

        Assert.Equal(vehicle.VehicleId, createdMaintenance.Vehicle.Id);
        Assert.Equal(request.description, createdMaintenance.Description);
        Assert.Equal(request.technicianName, createdMaintenance.TechnicianName);
    }

    [Fact]
    public async Task CreateMaintenance_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/private/vehicles/1/maintenances", new
        {
            serviceDate = DateTime.UtcNow.Date,
            description = "Inspection",
            technicianName = "Alex Turner",
            cost = 10m,
            nextInspectionMileage = 1000
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateMaintenance_ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync("/api/private/vehicles/1/maintenances", new
        {
            serviceDate = DateTime.UtcNow.Date,
            description = "Inspection",
            technicianName = "Alex Turner",
            cost = 10m,
            nextInspectionMileage = 1000
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateMaintenance_ReturnsBadRequest_WhenRequestIsInvalid()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var response = await _client.PostAsJsonAsync($"/api/private/vehicles/{vehicle.VehicleId}/maintenances", new
        {
            serviceDate = DateTime.UtcNow.Date.AddDays(1),
            description = "",
            technicianName = "",
            cost = -1m,
            nextInspectionMileage = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMaintenance_ReturnsNotFound_WhenVehicleDoesNotExist()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsJsonAsync("/api/private/vehicles/999999/maintenances", new
        {
            serviceDate = DateTime.UtcNow.Date,
            description = "Inspection",
            technicianName = "Alex Turner",
            cost = 10m,
            nextInspectionMileage = 1000
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
