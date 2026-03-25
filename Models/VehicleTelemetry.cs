namespace Project.Models;

public class VehicleTelemetry
{
    public int Id {get;set;}
    public string GPS {get;set;}
    public double CurrentSpeed {get;set;}
    public double RemainingBatteryPercentage {get;set;}
    public double HardwareTemperature {get;set;}
    public DateTime TimeStamp {get;set;}
    public SensorDiagnostic SensorDiagnostic {get;set;}
    public Vehicle Vehicle {get;set;}   
}
