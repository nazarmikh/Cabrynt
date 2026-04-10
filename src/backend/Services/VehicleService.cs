using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Project.Services;

public interface IVehicleService
{
    Task<RegisterVehicleResponseDto?> RegisterVehicleAsync(RegisterVehicleRequestDto registerVehicleDto);
    // Task<Vehicle?> GetVehicleByIdAsync(int id);
}

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IPasswordHasher<User> _hasher;

    public VehicleService(IVehicleRepository vehicleRepository, IPassengerRepository passengerRepository, IPasswordHasher<User> hasher)
    {
        _vehicleRepository = vehicleRepository;
        _passengerRepository = passengerRepository;
        _hasher = hasher;
    }


    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _vehicleRepository.GetVehicleByIdAsync(id);
    }

    public async Task<RegisterVehicleResponseDto?> RegisterVehicleAsync(RegisterVehicleRequestDto registerVehicleDto)
    {
    
        
        User? existingVehicle = await _passengerRepository.GetUserByEmailAsync(registerVehicleDto.SystemEmail);
        if (existingVehicle != null)
        {
            throw new InvalidOperationException("A vehicle with this email already exists.");
        }
        
        User user = new User
        {
            Email = registerVehicleDto.SystemEmail,
            PasswordHash = "",
            Role = Role.Vehicle,
            AccountCreated = DateTime.UtcNow
        };
        user.PasswordHash = HashPassword(registerVehicleDto.SystemPassword, user);

        await _passengerRepository.AddUserAsync(user);

        var vehicle = new Vehicle
        {
            VIN = registerVehicleDto.VIN,
            LicencePlate = registerVehicleDto.LicencePlate,
            Model = registerVehicleDto.Model,
            VehicleType = registerVehicleDto.VehicleType,
            Year = registerVehicleDto.Year,
            VehicleStatus = VehicleStatus.Active,
            User = user
        };


        await _vehicleRepository.AddVehicleAsync(vehicle);
        await _vehicleRepository.UpdateDbAsync();

        return new RegisterVehicleResponseDto
        {
            VehicleId = vehicle.Id,
            VIN = vehicle.VIN,
            LicencePlate = vehicle.LicencePlate,
            Model = vehicle.Model,
            VehicleType = vehicle.VehicleType,
            VehicleStatus = vehicle.VehicleStatus,
            Year = vehicle.Year
        };
    }

    public string HashPassword(string password, User user)
    {
        string hashedPassword = _hasher.HashPassword(user, password);
        return hashedPassword;
    }
}



