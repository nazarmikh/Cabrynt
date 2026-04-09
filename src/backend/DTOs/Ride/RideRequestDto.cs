namespace Project.DTOs;

public class RideRequestDto
{
    public required string DepartureLocation { get; set; }
    public required string DestinationLocation { get; set; }
}