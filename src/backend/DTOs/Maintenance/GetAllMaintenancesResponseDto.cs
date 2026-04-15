namespace Project.DTOs;

public class GetAllMaintenancesResponseDto
{
    public IEnumerable<CreateMaintenanceResponseDto> Maintenances { get; set; } =
        Enumerable.Empty<CreateMaintenanceResponseDto>();
}
