namespace Project.Repositories;

public interface IVehicleRepository
{
    Task UpdateDbAsync();
    Task<Vehicle?> GetVehicleByIdAsync(int id);
    Task AddVehicleAsync(Vehicle vehicle);
    Task<bool> ExistsByVinAsync(string vin);
    Task<bool> ExistsByLicencePlateAsync(string licencePlate);
}

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddVehicleAsync(Vehicle vehicle)
    {
        await _context.Vehicles.AddAsync(vehicle);
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _context.Vehicles.FindAsync(id);
    }

    public async Task UpdateDbAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByVinAsync(string vin)
    {
        return await _context.Vehicles.AnyAsync(v => v.VIN == vin);
    }

    public async Task<bool> ExistsByLicencePlateAsync(string licencePlate)
    {
        return await _context.Vehicles.AnyAsync(v => v.LicencePlate == licencePlate);
    }

}
