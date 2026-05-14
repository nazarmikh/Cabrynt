namespace Project.Repositories;

public interface IPassengerRepository
{
    public Task SaveChangesAsync();
    public Task<User?> GetUserByEmailAsync(string email);
    public Task<User?> GetUserByIdAsync(int userId);
    public Task AddUserAsync(User user);
    public Task<PassengerProfile?> GetPassengerByIdAsync(int userId);
    public Task AddPassengerAsync(PassengerProfile passenger);
    public Task UpdatePassengerLoyaltyPointsByIdAsync(int userId, int newLoyaltyPoints);
}

public class PassengerRepository : IPassengerRepository
{
    private readonly AppDbContext _appDbContext;
    public PassengerRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task SaveChangesAsync()
    {
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(i => i.Id == userId);
    }

    public async Task<PassengerProfile?> GetPassengerByIdAsync(int userId)
    {
        return await _appDbContext.PassengerProfiles.Include(u => u.User).FirstOrDefaultAsync(id => id.UserId == userId);
    }

    public async Task AddUserAsync(User user)
    {
        await _appDbContext.Users.AddAsync(user);
    }

    public async Task AddPassengerAsync(PassengerProfile passenger)
    {
        await _appDbContext.PassengerProfiles.AddAsync(passenger);
    }

    public async Task UpdatePassengerLoyaltyPointsByIdAsync(int userId, int newLoyaltyPoints)
    {
        PassengerProfile? passenger = await _appDbContext.PassengerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (passenger != null)
        {
            passenger.Points = newLoyaltyPoints;
        }

    }

}

