using System.Security.Claims;
using Project.Endpoints;


namespace Project.Services;

public interface IRideService
{
    Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
    Task<RideQuoteResponseDto?> GetRideQuoteAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
    Task<List<RideResponseDto>?> GetAllRidesAsync(ClaimsPrincipal principal);
    Task<GetRideByIdResponseDto?> GetRideByIdAsync(ClaimsPrincipal principal, int rideId);
    Task CheckAvailableVehicleAsync(int rideId);
    Task<CompleteRideResponseDto?> CompleteRideAsync(ClaimsPrincipal principal, int rideId);
}

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IPriceService _priceService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<RideService> _logger;

    public RideService(IRideRepository rideRepository, IPassengerRepository passengerRepository, IPriceService priceService, IPaymentService paymentService, ILogger<RideService> logger)
    {
        _rideRepository = rideRepository;
        _passengerRepository = passengerRepository;
        _priceService = priceService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest)
    {
        var passenger = await GetPassengerAsync(principal);
        if (passenger is null)
            return null;

        int? vehicleId = await _rideRepository.GetNearestVehicleAsync(
            rideRequest.DepartureLatitude,
            rideRequest.DepartureLongitude,
            rideRequest.PreferredVehicleType);


        Vehicle? vehicle = null;

        if (vehicleId is not null)
        {
            vehicle = await _rideRepository.GetVehicleById(vehicleId.Value);
        }

        decimal distance = CalculateDistanceKm(
                rideRequest.DepartureLatitude,
                rideRequest.DepartureLongitude,
                rideRequest.DestinationLatitude,
                rideRequest.DestinationLongitude
        );

        decimal duration = CalculateEstimatedDurationMinutes(distance);
        DiscountCode? discountCode = await ResolveDiscountCodeAsync(rideRequest.DiscountCode);

        Ride ride = new Ride()
        {
            DepartureLocation = rideRequest.DepartureLocation,
            DepartureLatitude = rideRequest.DepartureLatitude,
            DepartureLongitude = rideRequest.DepartureLongitude,
            DestinationLocation = rideRequest.DestinationLocation,
            DestinationLatitude = rideRequest.DestinationLatitude,
            DestinationLongitude = rideRequest.DestinationLongitude,
            Distance = distance,
            Duration = duration,
            DiscountCode = discountCode,
            EstimatedPrice = 0,
            RequestTime = DateTime.UtcNow,
            PassengerProfile = passenger,
            RideStatus = RideStatus.Requested,
            PreferredVehicleType = rideRequest.PreferredVehicleType,
            Vehicle = null
        };

        decimal price = _priceService.EstimatePrice(
            distance,
            duration,
            rideRequest.PreferredVehicleType,
            DateTime.UtcNow,
            passenger.Points,
            discountCode);

        ride.EstimatedPrice = price;

        if (vehicle is not null)
        {
            ride.RideStatus = RideStatus.InProgress;
            ride.Vehicle = vehicle;
            vehicle.VehicleStatus = VehicleStatus.InRide;
        }

        await _rideRepository.AddRideAsync(ride);
        await _rideRepository.SaveChangesAsync();

        if (vehicle is null)
        {
            _logger.LogWarning(
                "Ride {RideId} created for passenger {PassengerUserId} without an available vehicle; ride remains {RideStatus}",
                ride.Id,
                passenger.UserId,
                ride.RideStatus);
        }
        else
        {
            _logger.LogInformation(
                "Ride {RideId} created for passenger {PassengerUserId} and assigned vehicle {VehicleId}",
                ride.Id,
                passenger.UserId,
                vehicle.Id);
        }

        RideResponseDto response = new RideResponseDto()
        {
            RideId = ride.Id,
            RideStatus = ride.RideStatus,
            RequestTime = ride.RequestTime,
            VehicleId = vehicleId,
            EstimatedPrice = ride.EstimatedPrice,
            DepartureLocation = ride.DepartureLocation,
            DestinationLocation = ride.DestinationLocation,
            Distance = ride.Distance,
            Duration = ride.Duration,
            PreferredVehicleType = ride.PreferredVehicleType
        };

        return response;
    }

    public async Task<RideQuoteResponseDto?> GetRideQuoteAsync(ClaimsPrincipal principal, RideRequestDto rideRequest)
    {
        var passenger = await GetPassengerAsync(principal);
        if (passenger is null)
        {
            return null;
        }

        var distance = CalculateDistanceKm(
            rideRequest.DepartureLatitude,
            rideRequest.DepartureLongitude,
            rideRequest.DestinationLatitude,
            rideRequest.DestinationLongitude);
        var duration = CalculateEstimatedDurationMinutes(distance);
        var discountCode = await ResolveDiscountCodeAsync(rideRequest.DiscountCode);
        var breakdown = _priceService.GetEstimatedBreakdown(
            distance,
            duration,
            rideRequest.PreferredVehicleType,
            DateTime.UtcNow,
            passenger.Points,
            discountCode);

        return new RideQuoteResponseDto
        {
            Distance = breakdown.Distance,
            Duration = breakdown.Duration,
            BaseFare = breakdown.StartingRate,
            DistanceCost = breakdown.DistanceCost,
            DurationCost = breakdown.DurationCost,
            VehicleMultiplier = breakdown.VehicleMultiplier,
            NightSurcharge = breakdown.NightSurcharge,
            IsNightRateApplied = breakdown.IsNightRateApplied,
            LoyaltyDiscount = breakdown.LoyaltyDiscount,
            CodeDiscount = breakdown.CodeDiscount,
            VatAmount = breakdown.VatAmount,
            EstimatedPrice = breakdown.Total
        };
    }

    public async Task<List<RideResponseDto>?> GetAllRidesAsync(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        List<Ride> rides = await _rideRepository.GetAllRidesAsync(userId);
        List<RideResponseDto> response = new List<RideResponseDto>();
        foreach (var item in rides)
        {
            RideResponseDto newRecord = new RideResponseDto()
            {
                RideId = item.Id,
                RideStatus = item.RideStatus,
                RequestTime = item.RequestTime,
                VehicleId = item.Vehicle?.Id,
                EstimatedPrice = item.EstimatedPrice,
                DepartureLocation = item.DepartureLocation,
                DestinationLocation = item.DestinationLocation,
                Distance = item.Distance,
                Duration = item.Duration,
                PreferredVehicleType = item.PreferredVehicleType
            };
            response.Add(newRecord);
        }


        // return rides;
        return response;
    }

    public async Task<GetRideByIdResponseDto?> GetRideByIdAsync(ClaimsPrincipal principal, int rideId)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;


        Ride? ride = await _rideRepository.GetRideByIdAsync(rideId);
        if (ride is null)
        {
            return null;
        }

        if (userId != ride.PassengerProfile.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        GetRideByIdResponseDto response = new GetRideByIdResponseDto
        {
            Id = ride.Id,
            DepartureLocation = ride.DepartureLocation,
            DestinationLocation = ride.DestinationLocation,
            RequestTime = ride.RequestTime,
            RideStatus = ride.RideStatus,
            VehicleModel = ride.Vehicle?.Model
        };

        return response;
    }

    public async Task CheckAvailableVehicleAsync(int rideId)
    {
        Ride? ride = await _rideRepository.GetRideByIdAsync(rideId);
        if (ride is null)
        {
            throw new InvalidOperationException("Ride not found");
        }

        int? vehicleId = await _rideRepository.GetNearestVehicleAsync(
            ride.DepartureLatitude,
            ride.DepartureLongitude,
            ride.PreferredVehicleType);
        if (vehicleId is null)
        {
            _logger.LogInformation("Ride {RideId} still has no available vehicle during assignment check", ride.Id);
            return;
        }

        Vehicle? vehicle = await _rideRepository.GetVehicleById(vehicleId.Value);
        if (vehicle is null)
        {
            throw new InvalidOperationException("Vehicle with that id is not found");
        }

        ride.RideStatus = RideStatus.InProgress;
        ride.Vehicle = vehicle;
        vehicle.VehicleStatus = VehicleStatus.InRide;
        _rideRepository.UpdateRide(ride);
        await _rideRepository.SaveChangesAsync();

        _logger.LogInformation("Ride {RideId} was assigned vehicle {VehicleId} after waiting", ride.Id, vehicle.Id);

    }

    public async Task<CompleteRideResponseDto?> CompleteRideAsync(ClaimsPrincipal principal, int rideId)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        var ride = await _rideRepository.GetRideByIdAsync(rideId);
        if (ride is null)
        {
            throw new InvalidOperationException("Ride not found");
        }

        if (ride.Vehicle is null)
        {
            throw new InvalidOperationException("Ride has no assigned vehicle");
        }

        if (ride.RideStatus != RideStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress rides can be completed");
        }

        if (!principal.IsInRole("Admin") && ride.Vehicle.UserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        ride.RideStatus = RideStatus.Completed;
        ride.Vehicle.VehicleStatus = VehicleStatus.Active;

        var freedVehicle = ride.Vehicle;
        await AssignFreedVehicleToWaitingRideAsync(freedVehicle);

        _rideRepository.UpdateRide(ride);
        await _rideRepository.SaveChangesAsync();

        var payment = await _paymentService.CreatePaymentAsync(new CreatePaymentRequestDto
        {
            RideId = ride.Id
        });

        if (payment is null)
        {
            throw new InvalidOperationException("Failed to create payment for completed ride");
        }

        _logger.LogInformation(
            "Ride {RideId} completed by user {UserId}; payment {PaymentId} created and vehicle {VehicleId} processed for reassignment",
            ride.Id,
            userId,
            payment.Id,
            ride.Vehicle.Id);

        return new CompleteRideResponseDto
        {
            RideId = ride.Id,
            RideStatus = ride.RideStatus,
            CompletedAt = DateTime.UtcNow,
            VehicleId = ride.Vehicle.Id,
            PaymentId = payment.Id,
            PaymentAmount = payment.PayAmount
        };
    }

    private async Task AssignFreedVehicleToWaitingRideAsync(Vehicle vehicle)
    {
        var waitingRide = await _rideRepository.GetOldestRequestedRideAsync(vehicle.VehicleType);
        if (waitingRide is null)
        {
            _logger.LogInformation("Vehicle {VehicleId} became available with no waiting ride for type {VehicleType}", vehicle.Id, vehicle.VehicleType);
            return;
        }

        waitingRide.Vehicle = vehicle;
        waitingRide.RideStatus = RideStatus.InProgress;
        vehicle.VehicleStatus = VehicleStatus.InRide;

        _rideRepository.UpdateRide(waitingRide);

        _logger.LogInformation(
            "Vehicle {VehicleId} was reassigned to waiting ride {RideId} of type {VehicleType}",
            vehicle.Id,
            waitingRide.Id,
            vehicle.VehicleType);
    }

    private static decimal CalculateDistanceKm(
        double startLat,
        double startLon,
        double endLat,
        double endLon)
    {
        const double earthRadiusKm = 6371.0;

        static double ToRadians(double angle) => Math.PI * angle / 180.0;

        var dLat = ToRadians(endLat - startLat);
        var dLon = ToRadians(endLon - startLon);

        var lat1 = ToRadians(startLat);
        var lat2 = ToRadians(endLat);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = earthRadiusKm * c;

        return Math.Round((decimal)distance, 2);
    }

    private static decimal CalculateEstimatedDurationMinutes(decimal distanceKm)
    {
        const decimal averageSpeedKmPerHour = 40m;
        var hours = distanceKm / averageSpeedKmPerHour;
        return Math.Round(hours * 60m, 2);
    }

    private async Task<PassengerProfile?> GetPassengerAsync(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        return await _passengerRepository.GetPassengerByIdAsync(userId);
    }

    private async Task<DiscountCode?> ResolveDiscountCodeAsync(string? discountCodeValue)
    {
        if (string.IsNullOrWhiteSpace(discountCodeValue))
        {
            return null;
        }

        var discountCode = await _rideRepository.GetDiscountCodeByCodeAsync(discountCodeValue.Trim());
        if (discountCode is null)
        {
            _logger.LogWarning("Ride creation rejected because discount code {DiscountCode} was not found", discountCodeValue.Trim());
            throw new InvalidOperationException("Invalid discount code");
        }

        if (discountCode.ExpirationDate <= DateTime.UtcNow)
        {
            _logger.LogWarning("Ride creation rejected because discount code {DiscountCode} is expired", discountCode.Code);
            throw new InvalidOperationException("Discount code has expired");
        }

        if (!discountCode.IsActive)
        {
            _logger.LogWarning("Ride creation rejected because discount code {DiscountCode} is inactive", discountCode.Code);
            throw new InvalidOperationException("Discount code is not active");
        }

        return discountCode;
    }


}
