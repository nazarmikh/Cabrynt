using Project.Enums;

namespace Project.DTOs;

public class CompleteRideResponseDto
{
    public int RideId { get; set; }
    public RideStatus RideStatus { get; set; }
    public DateTime CompletedAt { get; set; }
    public int? VehicleId { get; set; }
    public int? PaymentId { get; set; }
    public decimal? PaymentAmount { get; set; }
}
