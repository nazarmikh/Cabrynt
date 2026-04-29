using Project.Enums;

namespace Project.DTOs;

public class RideResponseDto
{
    public int RideId {get;set;}
    public RideStatus RideStatus {get;set;}
    public DateTime RequestTime {get;set;}
    public decimal EstimatedPrice {get;set;}
    public int? VehicleId {get;set;}
    public string? DepartureLocation { get; set; }
    public string? DestinationLocation { get; set; }
    public decimal Distance { get; set; }
    public decimal Duration { get; set; }
    public VehicleType PreferredVehicleType { get; set; }
}

