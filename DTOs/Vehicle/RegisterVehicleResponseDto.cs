using Project.Enums;

namespace Project.DTOs;

public class RegisterVehicleResponseDto
{
    public int VehicleId { get; set; }
    public string VIN { get; set; } = string.Empty;
    public string LicencePlate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public int Year { get; set; }

}
