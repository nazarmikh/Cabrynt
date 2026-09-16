using System.Security.Claims;

namespace Project.Services;

public interface IRideService
{
    Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
    Task<RideQuoteResponseDto?> GetRideQuoteAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
    Task<List<RideResponseDto>?> GetAllRidesAsync(ClaimsPrincipal principal);
    Task<GetRideByIdResponseDto?> GetRideByIdAsync(ClaimsPrincipal principal, int rideId);
    Task<bool?> CancelRideAsync(ClaimsPrincipal principal, int rideId);
}

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IPriceService _priceService;
    private readonly IRouteEstimator _routeEstimator;
    private readonly ITripDurationEstimator _tripDurationEstimator;
    private readonly ILogger<RideService> _logger;

    public RideService(
        IRideRepository rideRepository,
        IPassengerRepository passengerRepository,
        IPriceService priceService,
        IRouteEstimator routeEstimator,
        ITripDurationEstimator tripDurationEstimator,
        ILogger<RideService> logger)
    {
        _rideRepository = rideRepository;
        _passengerRepository = passengerRepository;
        _priceService = priceService;
        _routeEstimator = routeEstimator;
        _tripDurationEstimator = tripDurationEstimator;
        _logger = logger;
    }

    public async Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest)
    {
        var passenger = await GetPassengerAsync(principal);
        if (passenger is null)
        {
            return null;
        }

        var routeEstimate = await EstimateRouteAsync(rideRequest);
        var distance = Math.Round((decimal)routeEstimate.DistanceKm, 2);
        var duration = Math.Round((decimal)routeEstimate.DurationMinutes, 2);

        var ride = new Ride
        {
            DepartureLocation = rideRequest.DepartureLocation,
            DepartureLatitude = rideRequest.DepartureLatitude,
            DepartureLongitude = rideRequest.DepartureLongitude,
            DestinationLocation = rideRequest.DestinationLocation,
            DestinationLatitude = rideRequest.DestinationLatitude,
            DestinationLongitude = rideRequest.DestinationLongitude,
            Distance = distance,
            Duration = duration,
            EstimatedPrice = _priceService.EstimatePrice(
                distance,
                duration,
                rideRequest.PreferredVehicleType,
                DateTime.UtcNow),
            RequestTime = DateTime.UtcNow,
            PassengerProfile = passenger,
            RideStatus = RideStatus.Requested,
            PreferredVehicleType = rideRequest.PreferredVehicleType
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

        var routeEstimate = await EstimateRouteAsync(rideRequest);
        var distance = Math.Round((decimal)routeEstimate.DistanceKm, 2);
        var duration = Math.Round((decimal)routeEstimate.DurationMinutes, 2);
        var quoteRequestedAt = DateTimeOffset.UtcNow;
        var tripDurationEstimate = await _tripDurationEstimator.EstimateAsync(
            CreateRouteRequest(rideRequest),
            routeEstimate,
            quoteRequestedAt);
        var breakdown = _priceService.GetEstimatedBreakdown(
            distance,
            duration,
            rideRequest.PreferredVehicleType,
            quoteRequestedAt.UtcDateTime);

        return new RideQuoteResponseDto
        {
            Distance = breakdown.Distance,
            Duration = breakdown.Duration,
            EstimatedTripDuration = Math.Round((decimal)tripDurationEstimate.DurationMinutes, 2),
            EstimatedTripDurationSource = tripDurationEstimate.Source,
            BaseFare = breakdown.StartingRate,
            DistanceCost = breakdown.DistanceCost,
            DurationCost = breakdown.DurationCost,
            VehicleMultiplier = breakdown.VehicleMultiplier,
            NightSurcharge = breakdown.NightSurcharge,
            IsNightRateApplied = breakdown.IsNightRateApplied,
            VatAmount = breakdown.VatAmount,
            EstimatedPrice = breakdown.Total
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

    public async Task<GetRideByIdResponseDto?> GetRideByIdAsync(ClaimsPrincipal principal, int rideId)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return null;
        }

        var ride = await _rideRepository.GetRideByIdAsync(rideId);
        if (ride is null)
        {
            return null;
        }

        if (userId != ride.PassengerProfile.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        return new GetRideByIdResponseDto
        {
            Id = ride.Id,
            DepartureLocation = ride.DepartureLocation,
            DestinationLocation = ride.DestinationLocation,
            RequestTime = ride.RequestTime,
            RideStatus = ride.RideStatus
        };
    }

    public async Task<bool?> CancelRideAsync(ClaimsPrincipal principal, int rideId)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return null;
        }

        var ride = await _rideRepository.GetRideByIdAsync(rideId);
        if (ride is null)
        {
            return false;
        }

        // Requests cannot be canceled on behalf of another passenger or after a future dispatch step.
        if (ride.PassengerProfile.UserId != userId || ride.RideStatus != RideStatus.Requested)
        {
            return false;
        }

        ride.RideStatus = RideStatus.Canceled;
        _rideRepository.UpdateRide(ride);
        await _rideRepository.SaveChangesAsync();

        _logger.LogInformation("Ride request {RideId} was canceled by passenger {PassengerUserId}", ride.Id, userId);
        return true;
    }

    private Task<RouteEstimate> EstimateRouteAsync(RideRequestDto rideRequest)
    {
        return _routeEstimator.EstimateAsync(CreateRouteRequest(rideRequest));
    }

    private static RouteRequest CreateRouteRequest(RideRequestDto rideRequest)
    {
        return new RouteRequest(
            rideRequest.DepartureLatitude,
            rideRequest.DepartureLongitude,
            rideRequest.DestinationLatitude,
            rideRequest.DestinationLongitude);
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
            DepartureLocation = ride.DepartureLocation,
            DestinationLocation = ride.DestinationLocation,
            Distance = ride.Distance,
            Duration = ride.Duration,
            PreferredVehicleType = ride.PreferredVehicleType
        };
    }
}
