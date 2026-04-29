namespace Project.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetTicketByIdAsync(int id);
    Task<List<Ticket>> GetAllTicketsAsync(int userId);
    Task<List<Ticket>> GetAllTicketsForAdminAsync();
    Task CreateTicketAsync(Ticket ticket);
    Task SaveChangesAsync();
}

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _appDbContext;
    public TicketRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task CreateTicketAsync(Ticket ticket)
    {
        await _appDbContext.Tickets.AddAsync(ticket);
    }

    public async Task<List<Ticket>> GetAllTicketsAsync(int userId)
    {
        return await _appDbContext.Tickets
            .Where(x => x.PassengerProfile.UserId == userId)
            .OrderByDescending(x => x.ReportTime)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetAllTicketsForAdminAsync()
    {
        return await _appDbContext.Tickets
            .Include(x => x.PassengerProfile)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.ReportTime)
            .ToListAsync();
    }

    public async Task<Ticket?> GetTicketByIdAsync(int id)
    {
        return await _appDbContext.Tickets
            .Include(x => x.PassengerProfile)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task SaveChangesAsync()
    {
        await _appDbContext.SaveChangesAsync();
    }
}
