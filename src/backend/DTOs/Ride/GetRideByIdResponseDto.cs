namespace Project.DTOs;

public class GetRideByIdResponseDto
{
    public int Id {get;set;}
    public required string DepartureLocation {get;set;}
    public required string DestinationLocation {get;set;}
    public RideStatus RideStatus {get;set;}
    public DateTime RequestTime {get;set;}
    public string? VehicleModel {get;set;}
    
}