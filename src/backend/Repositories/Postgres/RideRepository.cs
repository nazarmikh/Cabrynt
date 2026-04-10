using MongoDB.Driver;

namespace Project.Repositories;

public interface IRideRepository
{
    Task SaveChangesAsync();
    Task<Ride?> GetRideByIdAsync(int id);
    Task AddRideAsync(Ride ride);
    void UpdateRide(Ride ride);
    Task<int?> GetNearestVehicle(double latitude, double longitude);
    Task<Vehicle?> GetVehicleById(int id);
}

public class RideRepository : IRideRepository
{
    private readonly AppDbContext _appDbContext;
    private readonly TelemetryMongoContext _mongoContext;
    public RideRepository(AppDbContext appDbContext, TelemetryMongoContext mongoContext)
    {
        _appDbContext = appDbContext;
        _mongoContext = mongoContext;
    }

    public Task SaveChangesAsync()
    {
        return _appDbContext.SaveChangesAsync();
    }

    public Task<Ride?> GetRideByIdAsync(int id)
    {
        return _appDbContext.Rides.Include(x => x.Vehicle).Include(x => x.PassengerProfile).ThenInclude(x => x.User).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddRideAsync(Ride ride)
    {
        await _appDbContext.Rides.AddAsync(ride);
    }

    public void UpdateRide(Ride ride)
    {
        _appDbContext.Rides.Update(ride);
    }

    public async Task<int?> GetNearestVehicle(double latitude, double longitude)
    {
        var latestTelemetry = await _mongoContext.VehicleTelemetries
            .Aggregate()
            .SortByDescending(t => t.TimeStamp)
            .Group(
                t => t.VehicleId,
                g => new
                {
                    VehicleId = g.Key,
                    Latitude = g.First().Latitude,
                    Longitude = g.First().Longitude
                })
            .ToListAsync();

        var nearest = latestTelemetry
            .Select(t => new
            {
                t.VehicleId,
                Distance = Math.Pow(t.Latitude - latitude, 2) + Math.Pow(t.Longitude - longitude, 2)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return nearest?.VehicleId;
    }

    public async Task<Vehicle?> GetVehicleById(int id)
    {
        var vehicle = await _appDbContext.Vehicles.FindAsync(id);
        return vehicle;
    }


}