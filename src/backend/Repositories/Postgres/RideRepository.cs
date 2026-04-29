using MongoDB.Driver;

namespace Project.Repositories;

public interface IRideRepository
{
    Task SaveChangesAsync();
    Task<List<Ride>> GetAllRidesAsync(int passengerId);
    Task<Ride?> GetRideByIdAsync(int id);
    Task<Ride?> GetOldestRequestedRideAsync(VehicleType preferredVehicleType);
    Task<DiscountCode?> GetDiscountCodeByCodeAsync(string code);
    Task AddRideAsync(Ride ride);
    void UpdateRide(Ride ride);
    Task<int?> GetNearestVehicleAsync(double latitude, double longitude, VehicleType preferredVehicleType);
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
        return _appDbContext.Rides.Include(x => x.Vehicle).Include(x => x.PassengerProfile).ThenInclude(x => x.User).Include(x => x.DiscountCode).FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task<Ride?> GetOldestRequestedRideAsync(VehicleType preferredVehicleType)
    {
        return _appDbContext.Rides
            .Include(x => x.PassengerProfile)
            .ThenInclude(x => x.User)
            .Include(x => x.DiscountCode)
            .Where(r => r.RideStatus == RideStatus.Requested
                && r.Vehicle == null
                && r.PreferredVehicleType == preferredVehicleType)
            .OrderBy(r => r.RequestTime)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Ride>> GetAllRidesAsync(int passengerId)
    {
        return await _appDbContext.Rides
            .Include(x => x.Vehicle)
            .Include(x => x.PassengerProfile)
            .ThenInclude(x => x.User)
            .Include(x => x.DiscountCode)
            .Where(r => r.PassengerProfile.UserId == passengerId)
            .ToListAsync();
    }

    public async Task<DiscountCode?> GetDiscountCodeByCodeAsync(string code)
    {
        return await _appDbContext.DiscountCodes.FirstOrDefaultAsync(d => d.Code == code);
    }

    public void UpdateRide(Ride ride)
    {
        _appDbContext.Rides.Update(ride);
    }

    public async Task<int?> GetNearestVehicleAsync(double latitude, double longitude, VehicleType preferredVehicleType)
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

        var candidateVehicleIds = latestTelemetry.Select(t => t.VehicleId).ToList();

        var eligibleVehicles = await _appDbContext.Vehicles
            .Where(v => candidateVehicleIds.Contains(v.Id)
                && v.VehicleType == preferredVehicleType
                && v.VehicleStatus == VehicleStatus.Active)
            .Select(v => v.Id)
            .ToListAsync();

        var nearest = latestTelemetry
            .Where(t => eligibleVehicles.Contains(t.VehicleId))
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

    public async Task AddRideAsync(Ride ride)
    {
        await _appDbContext.Rides.AddAsync(ride);
    }
}
