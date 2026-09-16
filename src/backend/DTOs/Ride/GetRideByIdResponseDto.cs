namespace Project.DTOs;

public class GetRideByIdResponseDto
{
    public int Id { get; set; }
    public required string DepartureLocation { get; set; }
    public required string DestinationLocation { get; set; }
    public RideStatus RideStatus { get; set; }
    public DateTime RequestTime { get; set; }
    public decimal? EstimatedTripDuration { get; set; }
    public TripDurationEstimateSource? EstimatedTripDurationSource { get; set; }
    public string? TripDurationModelVersion { get; set; }
}
