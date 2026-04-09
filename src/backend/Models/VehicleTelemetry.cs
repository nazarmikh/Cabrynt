using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace Project.Models;


public class VehicleTelemetry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {get;set;}
    public required string Latitude {get;set;}
    public required string Longitude {get;set;}

    private double _currentSpeed;
    public double CurrentSpeed
    {
        get 
        {return _currentSpeed;}
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("Speed cannot be negative");
            }
            _currentSpeed = value;
        }
    }

    private double _remainingBatteryPercentage;
    public double RemainingBatteryPercentage
    {
        get {return _remainingBatteryPercentage;}
        set
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentOutOfRangeException("Battery cannot be out of [0,100]");
            }
            _remainingBatteryPercentage = value;
        }
    }

    public double HardwareTemperature {get;set;}
    public DateTime TimeStamp {get;set;}
    public int VehicleId {get;set;}   
}
