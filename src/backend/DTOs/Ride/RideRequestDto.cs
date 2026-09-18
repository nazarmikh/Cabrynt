namespace Project.DTOs;

public class RideRequestDto
{
    public double DepartureLatitude { get; set; }
    public double DepartureLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public VehicleType PreferredServiceTier { get; set; }
}
