namespace Project.DTOs;

public class GetMaintenanceByIdResponseDto
{
    public int Id { get; set; }
    public DateTime ServiceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int NextInspectionMileage { get; set; }
    public int VehicleId { get; set; }
}
