namespace Project.Repositories;

public interface IRideRepository
{
    Task SaveChangesAsync();
    Task<Ride?> GetRideByIdAsync(int id);
    Task AddRideAsync(Ride ride);
    void UpdateRide(Ride ride);
}

public class RideRepository : IRideRepository
{
    private readonly AppDbContext _appDbContext;
    public RideRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
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



}