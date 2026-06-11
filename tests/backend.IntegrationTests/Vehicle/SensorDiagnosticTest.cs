using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Project.Data;
using Project.Enums;

namespace backend.IntegrationTests.Telemetry;

public class SensorDiagnosticTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public SensorDiagnosticTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithoutCookies();
    }

    [Fact]
    public async Task AddSensorDiagnostic_ReturnsCreatedAndPersistsRecord_WhenRequestIsValid()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var vehicleToken = await IntegrationTestData.LoginAsync(_client, vehicle.Request.SystemEmail, vehicle.Request.SystemPassword);
        IntegrationTestData.Authorize(_client, vehicleToken);

        var request = new
        {
            SensorType = SensorType.Lidar,
            ErrorCode = 1201,
            DeviationSeverity = DeviationSeverity.Severe,
            RawSensorValue = "{\"pointCloudQuality\":0.42,\"fault\":\"occlusion\"}",
            VehicleTelemetryId = "telemetry-101",
            VehicleId = vehicle.VehicleId
        };

        var response = await _client.PostAsJsonAsync("/api/private/sensor-diagnostics", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var mongoContext = scope.ServiceProvider.GetRequiredService<TelemetryMongoContext>();
        var savedDiagnostic = await mongoContext.SensorDiagnostics
            .Find(x => x.VehicleId == vehicle.VehicleId && x.VehicleTelemetryId == request.VehicleTelemetryId)
            .SortByDescending(x => x.TimeStamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(savedDiagnostic);
        Assert.Equal(request.SensorType, savedDiagnostic!.SensorType);
        Assert.Equal(request.ErrorCode, savedDiagnostic.ErrorCode);
        Assert.Equal(request.DeviationSeverity, savedDiagnostic.DeviationSeverity);
        Assert.Equal(request.RawSensorValue, savedDiagnostic.RawSensorValue);
        Assert.Equal(request.VehicleTelemetryId, savedDiagnostic.VehicleTelemetryId);
        Assert.Equal(request.VehicleId, savedDiagnostic.VehicleId);
    }

    [Fact]
    public async Task AddSensorDiagnostic_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/private/sensor-diagnostics", new
        {
            SensorType = SensorType.Camera,
            ErrorCode = 1500,
            DeviationSeverity = DeviationSeverity.Low,
            RawSensorValue = "{\"frameDropRate\":0.02}",
            VehicleTelemetryId = "telemetry-1",
            VehicleId = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddSensorDiagnostic_ReturnsForbidden_WhenCallerIsPassenger()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync("/api/private/sensor-diagnostics", new
        {
            SensorType = SensorType.Radar,
            ErrorCode = 1600,
            DeviationSeverity = DeviationSeverity.Middle,
            RawSensorValue = "{\"signalNoise\":0.75}",
            VehicleTelemetryId = "telemetry-1",
            VehicleId = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddSensorDiagnostic_ReturnsBadRequest_WhenValuesAreInvalid()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var vehicle = await IntegrationTestData.RegisterVehicleAsync(_client);

        var vehicleToken = await IntegrationTestData.LoginAsync(_client, vehicle.Request.SystemEmail, vehicle.Request.SystemPassword);
        IntegrationTestData.Authorize(_client, vehicleToken);

        var response = await _client.PostAsJsonAsync("/api/private/sensor-diagnostics", new
        {
            SensorType = "InvalidSensor",
            ErrorCode = 0,
            DeviationSeverity = "InvalidSeverity",
            RawSensorValue = "",
            VehicleTelemetryId = "",
            VehicleId = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
