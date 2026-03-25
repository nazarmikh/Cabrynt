namespace Project.Models;
using Project.Enums;

public class SensorDiagnostic
{
    public int Id {get;set;}
    public SensorType SensorType {get;set;}
    public string ErrorCode {get;set;}
    public DeviationSeverity DeviationSeverity {get;set;}
    public DateTime TimeStamp {get;set;}
    public string RawSensorValue {get;set;}

}
