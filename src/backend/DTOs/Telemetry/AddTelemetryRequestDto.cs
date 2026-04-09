namespace Project.DTOs;

public class AddTelemetryRequestDto
{
    public required string Latitude {get;set;}
    public required string Longitude {get;set;}
    public double CurrentSpeed {get;set;}
    public double RemainingBatteryPercentage {get;set;}
    public double HardwareTemperature {get;set;}
    public int VehicleId {get;set;}
}
