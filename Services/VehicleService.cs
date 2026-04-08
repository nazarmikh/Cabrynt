using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Project.Services;

public interface IVehicleService
{
    Task<RegisterVehicleResponseDto?> RegisterVehicleAsync(RegisterVehicleRequestDto registerVehicleDto);
    // Task<Vehicle?> GetVehicleByIdAsync(int id);
}

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _vehicleRepository.GetVehicleByIdAsync(id);
    }

    public async Task<RegisterVehicleResponseDto?> RegisterVehicleAsync(RegisterVehicleRequestDto registerVehicleDto)
    {

        var vehicle = new Vehicle
        {
            VIN = registerVehicleDto.VIN,
            LicencePlate = registerVehicleDto.LicencePlate,
            Model = registerVehicleDto.Model,
            VehicleType = registerVehicleDto.VehicleType,
            Year = registerVehicleDto.Year,
            VehicleStatus = Enums.VehicleStatus.Active
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
            Year = vehicle.Year
        };
    }
}



