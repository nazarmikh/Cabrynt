using Project.Enums;

namespace Project.Models;

public class Ride
{
    public int Id { get; set; }
    public double DepartureLatitude { get; set; }
    public double DepartureLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public decimal Distance { get; set; }
    public decimal Duration { get; set; }
    public decimal? EstimatedTripDuration { get; set; }
    public TripDurationEstimateSource? EstimatedTripDurationSource { get; set; }
    public string? TripDurationModelVersion { get; set; }
    public VehicleType PreferredServiceTier { get; set; }
    public decimal EstimatedPrice { get; set; }
    public RideStatus RideStatus { get; set; }
    public DateTime RequestTime { get; set; }
    public required PassengerProfile PassengerProfile { get; set; }
}
