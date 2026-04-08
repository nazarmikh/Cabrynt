namespace Project.Services;


public interface ITelemetryService
{
    Task AddTelemetryAsync(AddTelemetryRequestDto addTelemetryRequestDto);
}

public class TelemetryService : ITelemetryService
{
    private readonly ITelemetryRepository _telemetryRepository;
    public TelemetryService(ITelemetryRepository telemetryRepository)
    {
        _telemetryRepository = telemetryRepository;
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
    }
}
