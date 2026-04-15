using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Project.Endpoints;


namespace Project.Services;

public interface IRideService
{
    Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
    Task<List<RideResponseDto>?> GetAllRidesAsync(ClaimsPrincipal principal);
    Task<GetRideByIdResponseDto?> GetRideByIdAsync(ClaimsPrincipal principal, int rideId);
    Task CheckAvailableVehicleAsync(int rideId);
}

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IPriceService _priceService;

    public RideService(IRideRepository rideRepository, IPassengerRepository passengerRepository, IPriceService priceService)
    {
        _rideRepository = rideRepository;
        _passengerRepository = passengerRepository;
        _priceService = priceService;
    }

    public async Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        var passenger = await _passengerRepository.GetPassengerByIdAsync(userId);
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
        DiscountCode? discountCode = null;

        if (!string.IsNullOrWhiteSpace(rideRequest.DiscountCode))
        {
            discountCode = await _rideRepository.GetDiscountCodeByCodeAsync(rideRequest.DiscountCode.Trim());
            if (discountCode is null)
            {
                throw new InvalidOperationException("Invalid discount code");
            }
            if (discountCode.ExpirationDate <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Discount code has expired");
            }
            if (!discountCode.IsActive)
            {
                throw new InvalidOperationException("Discount code is not active");
            }
        }

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
        }

        await _rideRepository.AddRideAsync(ride);
        await _rideRepository.SaveChangesAsync(); 

        RideResponseDto response = new RideResponseDto()
        {
            RideId = ride.Id,
            RideStatus = ride.RideStatus,
            RequestTime = ride.RequestTime,
            VehicleId = vehicleId,
            EstimatedPrice = ride.EstimatedPrice
        };

        return response;
    }

    public async Task<List<RideResponseDto>?> GetAllRidesAsync(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        List<Ride> rides = await _rideRepository.GetAllRidesAsync(userId);
        List<RideResponseDto> response = new List<RideResponseDto>();
        foreach(var item in rides)
        {
            RideResponseDto newRecord = new RideResponseDto()
            {
                RideId = item.Id,
                RideStatus = item.RideStatus,
                RequestTime = item.RequestTime,
                VehicleId = item.Vehicle?.Id,
                EstimatedPrice = item.EstimatedPrice
            };
            response.Add(newRecord);
        }


        // return rides;
        return response;
    }

    public async Task<GetRideByIdResponseDto?> GetRideByIdAsync(ClaimsPrincipal principal, int rideId)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

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
            return;
        }

        Vehicle? vehicle = await _rideRepository.GetVehicleById(vehicleId.Value);
        if (vehicle is null)
        {
            throw new InvalidOperationException("Vehicle with that id is not found");
        }
        
        ride.RideStatus = RideStatus.InProgress;
        ride.Vehicle = vehicle;
        _rideRepository.UpdateRide(ride);
        await _rideRepository.SaveChangesAsync();

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


}
