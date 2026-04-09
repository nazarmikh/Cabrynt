using Project.Enums;

namespace Project.DTOs;

public class RegisterVehicleRequestDto
{
    public required string VIN { get; set; }
    public  required string LicencePlate {get;set;}
    public required string Model { get; set; }
    public VehicleType VehicleType {get;set;}
    public int Year { get; set; }
}