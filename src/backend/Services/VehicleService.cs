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
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(IVehicleRepository vehicleRepository, IPassengerRepository passengerRepository, IPasswordHasher<User> hasher, ILogger<VehicleService> logger)
    {
        _vehicleRepository = vehicleRepository;
        _passengerRepository = passengerRepository;
        _hasher = hasher;
        _logger = logger;
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
            _logger.LogWarning("Vehicle registration rejected because system email {Email} already exists", registerVehicleDto.SystemEmail);
            throw new InvalidOperationException("A vehicle with this email already exists.");
        }

        if (await _vehicleRepository.ExistsByVinAsync(registerVehicleDto.VIN)
            || await _vehicleRepository.ExistsByLicencePlateAsync(registerVehicleDto.LicencePlate))
        {
            _logger.LogWarning("Vehicle registration rejected because VIN {Vin} or licence plate {LicencePlate} already exists", registerVehicleDto.VIN, registerVehicleDto.LicencePlate);
            throw new InvalidOperationException("A vehicle with the same VIN or licence plate already exists.");
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

        _logger.LogInformation(
            "Vehicle {VehicleId} registered with type {VehicleType}, plate {LicencePlate}, and system user {UserId}",
            vehicle.Id,
            vehicle.VehicleType,
            vehicle.LicencePlate,
            user.Id);

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



