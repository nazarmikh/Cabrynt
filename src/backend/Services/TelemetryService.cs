namespace Project.Services;

using System.Text.Json;
using Project.Enums;


public interface ITelemetryService
{
    Task AddTelemetryAsync(AddTelemetryRequestDto addTelemetryRequestDto);
}

public class TelemetryService : ITelemetryService
{
    private const double HighSpeedThreshold = 160;
    private const double CriticalBatteryThreshold = 5;
    private const double HighHardwareTemperatureThreshold = 85;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ISensorDiagnosticService _sensorDiagnosticService;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(ITelemetryRepository telemetryRepository, ISensorDiagnosticService sensorDiagnosticService, ILogger<TelemetryService> logger)
    {
        _telemetryRepository = telemetryRepository;
        _sensorDiagnosticService = sensorDiagnosticService;
        _logger = logger;
    }

    public async Task AddTelemetryAsync(AddTelemetryRequestDto addTelemetryRequestDto)
    {
        VehicleTelemetry vehicleTelemetry = new VehicleTelemetry()
        {
            Latitude = addTelemetryRequestDto.Latitude,
            Longitude = addTelemetryRequestDto.Longitude,
            CurrentSpeed = addTelemetryRequestDto.CurrentSpeed,
            RemainingBatteryPercentage = addTelemetryRequestDto.RemainingBatteryPercentage,
            HardwareTemperature = addTelemetryRequestDto.HardwareTemperature,
            VehicleId = addTelemetryRequestDto.VehicleId,
            TimeStamp = DateTime.UtcNow
        };

        await _telemetryRepository.AddTelemetryAsync(vehicleTelemetry);
        _logger.LogInformation(
            "Telemetry stored for vehicle {VehicleId} at {Timestamp}; speed {Speed}, battery {Battery}, hardware temperature {Temperature}",
            vehicleTelemetry.VehicleId,
            vehicleTelemetry.TimeStamp,
            vehicleTelemetry.CurrentSpeed,
            vehicleTelemetry.RemainingBatteryPercentage,
            vehicleTelemetry.HardwareTemperature);

        await CreateThresholdDiagnosticsAsync(vehicleTelemetry);
    }

    private async Task CreateThresholdDiagnosticsAsync(VehicleTelemetry vehicleTelemetry)
    {
        if (vehicleTelemetry.CurrentSpeed >= HighSpeedThreshold)
        {
            _logger.LogWarning(
                "Telemetry threshold exceeded for vehicle {VehicleId}: speed {Speed} >= {Threshold}",
                vehicleTelemetry.VehicleId,
                vehicleTelemetry.CurrentSpeed,
                HighSpeedThreshold);
            await _sensorDiagnosticService.AddSensorDiagnosticAsync(new CreateSensorDiagnosticRequestDto
            {
                SensorType = SensorType.Lidar,
                ErrorCode = 9301,
                DeviationSeverity = DeviationSeverity.Low,
                RawSensorValue = JsonSerializer.Serialize(new
                {
                    Trigger = "HighSpeedThresholdExceeded",
                    Threshold = HighSpeedThreshold,
                    Actual = vehicleTelemetry.CurrentSpeed
                }),
                VehicleTelemetryId = vehicleTelemetry.Id,
                VehicleId = vehicleTelemetry.VehicleId
            });
        }

        if (vehicleTelemetry.RemainingBatteryPercentage <= CriticalBatteryThreshold)
        {
            _logger.LogWarning(
                "Telemetry threshold exceeded for vehicle {VehicleId}: battery {Battery} <= {Threshold}",
                vehicleTelemetry.VehicleId,
                vehicleTelemetry.RemainingBatteryPercentage,
                CriticalBatteryThreshold);
            await _sensorDiagnosticService.AddSensorDiagnosticAsync(new CreateSensorDiagnosticRequestDto
            {
                SensorType = SensorType.Radar,
                ErrorCode = 9201,
                DeviationSeverity = DeviationSeverity.Middle,
                RawSensorValue = JsonSerializer.Serialize(new
                {
                    Trigger = "CriticalBatteryThresholdExceeded",
                    Threshold = CriticalBatteryThreshold,
                    Actual = vehicleTelemetry.RemainingBatteryPercentage
                }),
                VehicleTelemetryId = vehicleTelemetry.Id,
                VehicleId = vehicleTelemetry.VehicleId
            });
        }

        if (vehicleTelemetry.HardwareTemperature >= HighHardwareTemperatureThreshold)
        {
            _logger.LogWarning(
                "Telemetry threshold exceeded for vehicle {VehicleId}: hardware temperature {Temperature} >= {Threshold}",
                vehicleTelemetry.VehicleId,
                vehicleTelemetry.HardwareTemperature,
                HighHardwareTemperatureThreshold);
            await _sensorDiagnosticService.AddSensorDiagnosticAsync(new CreateSensorDiagnosticRequestDto
            {
                SensorType = SensorType.Camera,
                ErrorCode = 9101,
                DeviationSeverity = DeviationSeverity.Severe,
                RawSensorValue = JsonSerializer.Serialize(new
                {
                    Trigger = "HighHardwareTemperatureThresholdExceeded",
                    Threshold = HighHardwareTemperatureThreshold,
                    Actual = vehicleTelemetry.HardwareTemperature
                }),
                VehicleTelemetryId = vehicleTelemetry.Id,
                VehicleId = vehicleTelemetry.VehicleId
            });
        }
    }
}
