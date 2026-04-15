using Project.Enums;

namespace Project.Models;

public class Ride
{
    public int Id {get;set;}
    public required string DepartureLocation {get;set;}
    public double DepartureLatitude {get;set;}
    public double DepartureLongitude {get;set;}
    public required string DestinationLocation {get;set;}
    public double DestinationLatitude {get;set;}
    public double DestinationLongitude {get;set;}
    public decimal Distance {get;set;}
    public decimal Duration {get;set;}
    public VehicleType PreferredVehicleType {get;set;}
    public decimal EstimatedPrice {get;set;}
    public DiscountCode? DiscountCode {get;set;}
    public RideStatus RideStatus {get;set;}
    public DateTime RequestTime {get;set;}
    public required PassengerProfile PassengerProfile {get;set;}
    public Vehicle? Vehicle {get;set;} 
}
