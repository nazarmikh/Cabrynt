using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Project.Data;
using Project.Enums;

namespace backend.IntegrationTests.Telemetry;

public class TelemetryTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TelemetryTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithoutCookies();
    }

    [Fact]
    public async Task AddTelemetry_ReturnsCreatedAndPersistsRecord_WhenRequestIsValid()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var vehicleToken = await IntegrationTestData.LoginAsync(_client, vehicle.Request.SystemEmail, vehicle.Request.SystemPassword);
        IntegrationTestData.Authorize(_client, vehicleToken);

        var random = new Random();
        var request = new
        {
            Latitude = random.NextDouble() * 180 - 90,
            Longitude = random.NextDouble() * 360 - 180,
            CurrentSpeed = random.Next(0, 120),
            RemainingBatteryPercentage = random.Next(0, 101),
            HardwareTemperature = random.Next(5, 70),
            VehicleId = vehicle.VehicleId
        };

        var addTelemetryResponse = await _client.PostAsJsonAsync("/api/private/telemetry", request);

        Assert.Equal(HttpStatusCode.Created, addTelemetryResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var mongoContext = scope.ServiceProvider.GetRequiredService<TelemetryMongoContext>();
        var savedTelemetry = await mongoContext.VehicleTelemetries
            .Find(x => x.VehicleId == vehicle.VehicleId)
            .SortByDescending(x => x.TimeStamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedTelemetry);
        Assert.Equal(request.Latitude, savedTelemetry!.Latitude);
        Assert.Equal(request.Longitude, savedTelemetry.Longitude);
        Assert.Equal(request.CurrentSpeed, savedTelemetry.CurrentSpeed);
        Assert.Equal(request.RemainingBatteryPercentage, savedTelemetry.RemainingBatteryPercentage);
        Assert.Equal(request.HardwareTemperature, savedTelemetry.HardwareTemperature);
    }

    [Fact]
    public async Task AddTelemetry_ReturnsForbidden_WhenCallerIsPassenger()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync("/api/private/telemetry", new
        {
            Latitude = 50.85,
            Longitude = 4.35,
            CurrentSpeed = 25,
            RemainingBatteryPercentage = 70,
            HardwareTemperature = 35,
            VehicleId = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddTelemetry_ReturnsBadRequest_WhenValuesAreInvalid()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var vehicleToken = await IntegrationTestData.LoginAsync(_client, vehicle.Request.SystemEmail, vehicle.Request.SystemPassword);
        IntegrationTestData.Authorize(_client, vehicleToken);

        var response = await _client.PostAsJsonAsync("/api/private/telemetry", new
        {
            Latitude = 50.85,
            Longitude = 4.35,
            CurrentSpeed = -1,
            RemainingBatteryPercentage = 101,
            HardwareTemperature = 35,
            VehicleId = vehicle.VehicleId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddTelemetry_CreatesSensorDiagnostic_WhenThresholdIsExceeded()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var vehicleToken = await IntegrationTestData.LoginAsync(_client, vehicle.Request.SystemEmail, vehicle.Request.SystemPassword);
        IntegrationTestData.Authorize(_client, vehicleToken);

        var request = new
        {
            Latitude = 50.85,
            Longitude = 4.35,
            CurrentSpeed = 45,
            RemainingBatteryPercentage = 65,
            HardwareTemperature = 90,
            VehicleId = vehicle.VehicleId
        };

        var response = await _client.PostAsJsonAsync("/api/private/telemetry", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var mongoContext = scope.ServiceProvider.GetRequiredService<TelemetryMongoContext>();
        var savedTelemetry = await mongoContext.VehicleTelemetries
            .Find(x => x.VehicleId == vehicle.VehicleId)
            .SortByDescending(x => x.TimeStamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedTelemetry);

        var savedDiagnostic = await mongoContext.SensorDiagnostics
            .Find(x => x.VehicleId == vehicle.VehicleId && x.VehicleTelemetryId == savedTelemetry!.Id)
            .SortByDescending(x => x.TimeStamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedDiagnostic);
        Assert.Equal(SensorType.Camera, savedDiagnostic!.SensorType);
        Assert.Equal(9101, savedDiagnostic.ErrorCode);
        Assert.Equal(DeviationSeverity.Severe, savedDiagnostic.DeviationSeverity);
        Assert.Contains("HighHardwareTemperatureThresholdExceeded", savedDiagnostic.RawSensorValue);
    }
}
