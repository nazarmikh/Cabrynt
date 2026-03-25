using Project.Enums;

namespace Project.Models;

public class Ride
{
    public int Id {get;set;}
    public string DepartureLocation {get;set;}
    public string DestinationLocation {get;set;}
    public RideStatus RideStatus {get;set;}
    public DateTime RequestTime {get;set;}
    public Passenger? Passenger {get;set;} = null;
    public Vehicle? Vehicle {get;set;} = null;
}
