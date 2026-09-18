using System.Security.Claims;

namespace Project.Services;

public interface IRideService
{
    Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
    Task<RideQuoteResponseDto?> GetRideQuoteAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
    Task<List<RideResponseDto>?> GetAllRidesAsync(ClaimsPrincipal principal);
    Task<RideReadResult> GetRideByIdAsync(ClaimsPrincipal principal, int rideId);
    Task<RideRequestOperationStatus> CancelRideAsync(ClaimsPrincipal principal, int rideId);
}

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IQuoteService _quoteService;
    private readonly ILogger<RideService> _logger;

    public RideService(
        IRideRepository rideRepository,
        IPassengerRepository passengerRepository,
        IQuoteService quoteService,
        ILogger<RideService> logger)
    {
        _rideRepository = rideRepository;
        _passengerRepository = passengerRepository;
        _quoteService = quoteService;
        _logger = logger;
    }

    public async Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest)
    {
        var passenger = await GetPassengerAsync(principal);
        if (passenger is null)
        {
            return null;
        }

        var quote = await _quoteService.CalculateAsync(rideRequest, DateTimeOffset.UtcNow);

        var ride = new Ride
        {
            DepartureLatitude = rideRequest.DepartureLatitude,
            DepartureLongitude = rideRequest.DepartureLongitude,
            DestinationLatitude = rideRequest.DestinationLatitude,
            DestinationLongitude = rideRequest.DestinationLongitude,
            Distance = quote.Breakdown.Distance,
            Duration = quote.Breakdown.Duration,
            EstimatedTripDuration = quote.EstimatedTripDuration,
            EstimatedTripDurationSource = quote.EstimatedTripDurationSource,
            TripDurationModelVersion = quote.TripDurationModelVersion,
            EstimatedPrice = quote.Breakdown.Total,
            RequestTime = DateTime.UtcNow,
            PassengerProfile = passenger,
            RideStatus = RideStatus.Requested,
            PreferredServiceTier = rideRequest.PreferredServiceTier
        };

        await _rideRepository.AddRideAsync(ride);
        await _rideRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Ride request {RideId} created for passenger {PassengerUserId}",
            ride.Id,
            passenger.UserId);

        return MapRide(ride);
    }

    public async Task<RideQuoteResponseDto?> GetRideQuoteAsync(ClaimsPrincipal principal, RideRequestDto rideRequest)
    {
        var passenger = await GetPassengerAsync(principal);
        if (passenger is null)
        {
            return null;
        }

        var quote = await _quoteService.CalculateAsync(rideRequest, DateTimeOffset.UtcNow);

        return new RideQuoteResponseDto
        {
            Distance = quote.Breakdown.Distance,
            Duration = quote.Breakdown.Duration,
            EstimatedTripDuration = quote.EstimatedTripDuration,
            EstimatedTripDurationSource = quote.EstimatedTripDurationSource,
            BaseFare = quote.Breakdown.StartingRate,
            DistanceCost = quote.Breakdown.DistanceCost,
            DurationCost = quote.Breakdown.DurationCost,
            VehicleMultiplier = quote.Breakdown.VehicleMultiplier,
            NightSurcharge = quote.Breakdown.NightSurcharge,
            IsNightRateApplied = quote.Breakdown.IsNightRateApplied,
            VatAmount = quote.Breakdown.VatAmount,
            EstimatedPrice = quote.Breakdown.Total
        };
    }

    public async Task<List<RideResponseDto>?> GetAllRidesAsync(ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return null;
        }

        var rides = await _rideRepository.GetAllRidesAsync(userId);
        return rides.Select(MapRide).ToList();
    }

    public async Task<RideReadResult> GetRideByIdAsync(ClaimsPrincipal principal, int rideId)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return new RideReadResult(RideRequestOperationStatus.Unauthorized, null);
        }

        var ride = await _rideRepository.GetRideByIdAsync(rideId);
        if (ride is null)
        {
            return new RideReadResult(RideRequestOperationStatus.NotFound, null);
        }

        if (userId != ride.PassengerProfile.UserId)
        {
            return new RideReadResult(RideRequestOperationStatus.Forbidden, null);
        }

        return new RideReadResult(RideRequestOperationStatus.Success, MapRideDetails(ride));
    }

    public async Task<RideRequestOperationStatus> CancelRideAsync(ClaimsPrincipal principal, int rideId)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return RideRequestOperationStatus.Unauthorized;
        }

        var ride = await _rideRepository.GetRideByIdAsync(rideId);
        if (ride is null)
        {
            return RideRequestOperationStatus.NotFound;
        }

        if (ride.PassengerProfile.UserId != userId)
        {
            return RideRequestOperationStatus.Forbidden;
        }

        // Requests cannot be canceled after they have already left the requested state.
        if (ride.RideStatus != RideStatus.Requested)
        {
            return RideRequestOperationStatus.Conflict;
        }

        ride.RideStatus = RideStatus.Canceled;
        _rideRepository.UpdateRide(ride);
        await _rideRepository.SaveChangesAsync();

        _logger.LogInformation("Ride request {RideId} was canceled by passenger {PassengerUserId}", ride.Id, userId);
        return RideRequestOperationStatus.Success;
    }

    private async Task<PassengerProfile?> GetPassengerAsync(ClaimsPrincipal principal)
    {
        return !TryGetUserId(principal, out var userId)
            ? null
            : await _passengerRepository.GetPassengerByIdAsync(userId);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out int userId)
    {
        return int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private static RideResponseDto MapRide(Ride ride)
    {
        return new RideResponseDto
        {
            RideId = ride.Id,
            RideStatus = ride.RideStatus,
            RequestTime = ride.RequestTime,
            EstimatedPrice = ride.EstimatedPrice,
            DepartureLatitude = ride.DepartureLatitude,
            DepartureLongitude = ride.DepartureLongitude,
            DestinationLatitude = ride.DestinationLatitude,
            DestinationLongitude = ride.DestinationLongitude,
            Distance = ride.Distance,
            Duration = ride.Duration,
            EstimatedTripDuration = ride.EstimatedTripDuration,
            EstimatedTripDurationSource = ride.EstimatedTripDurationSource,
            TripDurationModelVersion = ride.TripDurationModelVersion,
            PreferredServiceTier = ride.PreferredServiceTier
        };
    }

    private static GetRideByIdResponseDto MapRideDetails(Ride ride)
    {
        return new GetRideByIdResponseDto
        {
            Id = ride.Id,
            DepartureLatitude = ride.DepartureLatitude,
            DepartureLongitude = ride.DepartureLongitude,
            DestinationLatitude = ride.DestinationLatitude,
            DestinationLongitude = ride.DestinationLongitude,
            RequestTime = ride.RequestTime,
            RideStatus = ride.RideStatus,
            EstimatedTripDuration = ride.EstimatedTripDuration,
            EstimatedTripDurationSource = ride.EstimatedTripDurationSource,
            TripDurationModelVersion = ride.TripDurationModelVersion
        };
    }
}
