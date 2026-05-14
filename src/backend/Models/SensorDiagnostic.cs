namespace Project.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Project.Enums;

public class SensorDiagnostic
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonRepresentation(BsonType.String)]
    public SensorType SensorType { get; set; }
    public int ErrorCode { get; set; }
    [BsonRepresentation(BsonType.String)]
    public DeviationSeverity DeviationSeverity { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? RawSensorValue { get; set; }
    public string? VehicleTelemetryId { get; set; }
    public int VehicleId { get; set; }

}
