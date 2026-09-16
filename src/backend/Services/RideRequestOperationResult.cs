namespace Project.Services;

public enum RideRequestOperationStatus
{
    Success,
    Unauthorized,
    NotFound,
    Forbidden,
    Conflict
}

public sealed record RideReadResult(
    RideRequestOperationStatus Status,
    GetRideByIdResponseDto? Ride);
