namespace Project.DTOs;

public class RideRequestDto
{
    public required string DepartureLocation { get; set; }
    public required string DestinationLocation { get; set; }
    public double DepartureLatitude { get; set; }
    public double DepartureLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public VehicleType PreferredVehicleType { get; set; }
    public string? DiscountCode { get; set; }
}
