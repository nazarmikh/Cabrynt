namespace Project.DTOs;

public class CreateSensorDiagnosticRequestDto
{
    public SensorType SensorType { get; set; }
    public int ErrorCode { get; set; }
    public DeviationSeverity DeviationSeverity { get; set; }
    public string? RawSensorValue { get; set; }
    public string? VehicleTelemetryId { get; set; }
    public int VehicleId { get; set; }

}
