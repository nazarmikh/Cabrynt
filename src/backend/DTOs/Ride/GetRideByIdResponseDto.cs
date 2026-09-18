namespace Project.DTOs;

public class GetRideByIdResponseDto
{
    public int Id { get; set; }
    public double DepartureLatitude { get; set; }
    public double DepartureLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public RideStatus RideStatus { get; set; }
    public DateTime RequestTime { get; set; }
    public decimal? EstimatedTripDuration { get; set; }
    public TripDurationEstimateSource? EstimatedTripDurationSource { get; set; }
    public string? TripDurationModelVersion { get; set; }
}
