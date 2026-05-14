namespace Project.DTOs;

public class CreateMaintenanceRequestDto
{
    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;
    public required string Description { get; set; }
    public required string TechnicianName { get; set; }
    public decimal Cost { get; set; }
    public int NextInspectionMileage { get; set; }
}