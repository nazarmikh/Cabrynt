using Project.Enums;

namespace Project.DTOs;

public class RideResponseDto
{
    public int RideId { get; set; }
    public RideStatus RideStatus { get; set; }
    public DateTime RequestTime { get; set; }
    public decimal EstimatedPrice { get; set; }
    public double DepartureLatitude { get; set; }
    public double DepartureLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public decimal Distance { get; set; }
    public decimal Duration { get; set; }
    public decimal? EstimatedTripDuration { get; set; }
    public TripDurationEstimateSource? EstimatedTripDurationSource { get; set; }
    public string? TripDurationModelVersion { get; set; }
    public VehicleType PreferredVehicleType { get; set; }
}

