using Project.Services;

namespace Project.DTOs;

public sealed class ModelDemoEstimateResponseDto
{
    public decimal RouteDistance { get; init; }
    public decimal RouteDuration { get; init; }
    public RouteEstimateSource RouteEstimateSource { get; init; }
    public decimal EstimatedTripDuration { get; init; }
    public TripDurationEstimateSource EstimatedTripDurationSource { get; init; }
    public decimal? ModelCorrection { get; init; }
    public string? ModelVersion { get; init; }
}
