using Project.Enums;

namespace Project.DTOs;

public class RideResponseDto
{
    public int RideId {get;set;}
    public RideStatus RideStatus {get;set;}
    public DateTime RequestTime {get;set;}
    public int? VehicleId {get;set;}
}

