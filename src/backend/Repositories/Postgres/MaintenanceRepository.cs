namespace Project.Repositories;

public interface IMaintenanceRepository
{
    Task AddMaintenanceAsync(Maintenance maintenance);
    Task<Maintenance?> GetMaintenanceByIdAsync(int id);
    Task<Maintenance?> GetMaintenanceByIdAsync(int vehicleId, int maintenanceId);
    Task<List<Maintenance>> GetMaintenancesByVehicleIdAsync(int vehicleId);
    Task SaveChangesAsync();
}

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly AppDbContext _appDbContext;
    public MaintenanceRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task AddMaintenanceAsync(Maintenance maintenance)
    {
        await _appDbContext.Maintenances.AddAsync(maintenance);
    }

    public async Task<Maintenance?> GetMaintenanceByIdAsync(int id)
    {
        return await _appDbContext.Maintenances.Include(m => m.Vehicle).FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Maintenance?> GetMaintenanceByIdAsync(int vehicleId, int maintenanceId)
    {
        return await _appDbContext.Maintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.Vehicle.Id == vehicleId);
    }

    public async Task<List<Maintenance>> GetMaintenancesByVehicleIdAsync(int vehicleId)
    {
        return await _appDbContext.Maintenances
            .Include(m => m.Vehicle)
            .Where(m => m.Vehicle.Id == vehicleId)
            .OrderByDescending(m => m.ServiceDate)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _appDbContext.SaveChangesAsync();
    }
}
