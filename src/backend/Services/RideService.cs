using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ZstdSharp.Unsafe;

namespace Project.Services;

public interface IRideService
{
    Task<RideResponseDto?> CreateRideAsync(ClaimsPrincipal principal, RideRequestDto rideRequest);
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

        int? vehicleId = await _rideRepository.GetNearestVehicle(rideRequest.DepartureLatitude, rideRequest.DepartureLongitude);
        if (vehicleId is null)
            throw new InvalidOperationException("No available vehicle found.");

        Vehicle? vehicle = await _rideRepository.GetVehicleById(vehicleId.Value);
        if (vehicle is null)
            throw new InvalidOperationException("Selected vehicle is not available.");

        Ride ride = new Ride()
        {
            DepartureLocation = rideRequest.DepartureLocation,
            DestinationLocation = rideRequest.DestinationLocation,
            RequestTime = DateTime.UtcNow,
            PassengerProfile = passenger,
            RideStatus = RideStatus.Requested,
            Vehicle = vehicle
        };

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

}
