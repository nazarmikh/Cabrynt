namespace Project.Services;

public interface IMaintenanceService
{
    Task<CreateMaintenanceResponseDto?> AddMaintenanceAsync(int vehicleId, CreateMaintenanceRequestDto request);
    Task<GetAllMaintenancesResponseDto?> GetAllMaintenancesAsync(int vehicleId);
    Task<GetMaintenanceByIdResponseDto?> GetMaintenanceByIdAsync(int vehicleId, int maintenanceId);
}

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(IMaintenanceRepository maintenanceRepository, IVehicleRepository vehicleRepository, ILogger<MaintenanceService> logger)
    {
        _maintenanceRepository = maintenanceRepository;
        _vehicleRepository = vehicleRepository;
        _logger = logger;
    }

    public async Task<CreateMaintenanceResponseDto?> AddMaintenanceAsync(int vehicleId, CreateMaintenanceRequestDto request)
    {
        Vehicle? vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
        if (vehicle is null)
            return null;


        Maintenance maintenance = new Maintenance()
        {
            Vehicle = vehicle,
            ServiceDate = request.ServiceDate,
            Description = request.Description,
            TechnicianName = request.TechnicianName,
            Cost = request.Cost,
            NextInspectionMileage = request.NextInspectionMileage
        };

        await _maintenanceRepository.AddMaintenanceAsync(maintenance);
        await _maintenanceRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Maintenance {MaintenanceId} created for vehicle {VehicleId} by technician {TechnicianName}",
            maintenance.Id,
            vehicleId,
            maintenance.TechnicianName);

        CreateMaintenanceResponseDto response = new CreateMaintenanceResponseDto()
        {
            Id = maintenance.Id,
            ServiceDate = maintenance.ServiceDate,
            Description = maintenance.Description,
            TechnicianName = maintenance.TechnicianName,
            Cost = maintenance.Cost,
            NextInspectionMileage = maintenance.NextInspectionMileage
        };
        return response;
    }

    public async Task<GetAllMaintenancesResponseDto?> GetAllMaintenancesAsync(int vehicleId)
    {
        Vehicle? vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
        if (vehicle is null)
            return null;

        var maintenances = await _maintenanceRepository.GetMaintenancesByVehicleIdAsync(vehicleId);

        return new GetAllMaintenancesResponseDto
        {
            Maintenances = maintenances.Select(m => new CreateMaintenanceResponseDto
            {
                Id = m.Id,
                ServiceDate = m.ServiceDate,
                Description = m.Description,
                TechnicianName = m.TechnicianName,
                Cost = m.Cost,
                NextInspectionMileage = m.NextInspectionMileage
            })
        };
    }

    public async Task<GetMaintenanceByIdResponseDto?> GetMaintenanceByIdAsync(int vehicleId, int maintenanceId)
    {
        Vehicle? vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
        if (vehicle is null)
            return null;

        var maintenance = await _maintenanceRepository.GetMaintenanceByIdAsync(vehicleId, maintenanceId);
        if (maintenance is null)
            return null;

        return new GetMaintenanceByIdResponseDto
        {
            Id = maintenance.Id,
            ServiceDate = maintenance.ServiceDate,
            Description = maintenance.Description,
            TechnicianName = maintenance.TechnicianName,
            Cost = maintenance.Cost,
            NextInspectionMileage = maintenance.NextInspectionMileage,
            VehicleId = maintenance.Vehicle.Id
        };
    }
}
