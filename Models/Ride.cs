using Project.Enums;

namespace Project.Models;

public class Ride
{
    public int Id {get;set;}
    public required string DepartureLocation {get;set;}
    public required string DestinationLocation {get;set;}
    public RideStatus RideStatus {get;set;}
    public DateTime RequestTime {get;set;}
    public required Passenger Passenger {get;set;}
    public Vehicle Vehicle {get;set;} 
}
