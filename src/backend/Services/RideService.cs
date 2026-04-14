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

    public RideService(IRideRepository rideRepository, IPassengerRepository passengerRepository)
    {
        _rideRepository = rideRepository;
        _passengerRepository = passengerRepository;
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

        int? vehicleId = await _rideRepository.GetNearestVehicleAsync(rideRequest.DepartureLatitude, rideRequest.DepartureLongitude);
        

        Vehicle? vehicle = null;

        if (vehicleId is not null)
        {
            vehicle = await _rideRepository.GetVehicleById(vehicleId.Value);
        }


        Ride ride = new Ride()
        {
            DepartureLocation = rideRequest.DepartureLocation,
            DepartureLatitude = rideRequest.DepartureLatitude,
            DepartureLongitude = rideRequest.DepartureLongitude,
            DestinationLocation = rideRequest.DestinationLocation,
            DestinationLatitude = rideRequest.DestinationLatitude,
            DestinationLongitude = rideRequest.DestinationLongitude,
            RequestTime = DateTime.UtcNow,
            PassengerProfile = passenger,
            RideStatus = RideStatus.Requested,
            Vehicle = null
        };
        
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
            VehicleId = vehicleId
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
                VehicleId = item.Vehicle?.Id
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

        int? vehicleId = await _rideRepository.GetNearestVehicleAsync(ride.DepartureLatitude, ride.DepartureLongitude);
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
}
