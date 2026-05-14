namespace Project.Services;

public interface ISensorDiagnosticService
{
    Task AddSensorDiagnosticAsync(CreateSensorDiagnosticRequestDto request);
}

public class SensorDiagnosticService : ISensorDiagnosticService
{
    private readonly ISensorDiagnosticRepository _sensorDiagnosticRepository;
    public SensorDiagnosticService(ISensorDiagnosticRepository sensorDiagnosticRepository)
    {
        _sensorDiagnosticRepository = sensorDiagnosticRepository;
    }
    public async Task AddSensorDiagnosticAsync(CreateSensorDiagnosticRequestDto request)
    {
        SensorDiagnostic sensorDiagnostic = new SensorDiagnostic()
        {
            SensorType = request.SensorType,
            ErrorCode = request.ErrorCode,
            DeviationSeverity = request.DeviationSeverity,
            TimeStamp = DateTime.UtcNow,
            RawSensorValue = request.RawSensorValue,
            VehicleTelemetryId = request.VehicleTelemetryId,
            VehicleId = request.VehicleId
        };

        await _sensorDiagnosticRepository.AddSensorDiagnosticAsync(sensorDiagnostic);
    }
}
