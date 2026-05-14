using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Project.Services;

public interface ITicketService
{
    Task<CreateTicketResponseDto?> CreateTicketAsync(ClaimsPrincipal principal, CreateTicketRequestDto ticketDto);
    Task<GetAllTicketsResponseDto?> GetAllTicketsAsync(ClaimsPrincipal principal);
    Task<GetTicketByIdResponseDto?> GetTicketByIdAsync(ClaimsPrincipal principal, int id);
    Task<List<AdminTicketResponseDto>> GetAdminTicketsAsync();
    Task<AdminTicketResponseDto?> UpdateTicketStatusAsync(int id, UpdateTicketStatusRequestDto request);
}

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly ILogger<TicketService> _logger;
    public TicketService(ITicketRepository ticketRepository, IPassengerRepository passengerRepository, ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _passengerRepository = passengerRepository;
        _logger = logger;
    }

    public async Task<CreateTicketResponseDto?> CreateTicketAsync(ClaimsPrincipal principal, CreateTicketRequestDto ticketDto)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        var passenger = await _passengerRepository.GetPassengerByIdAsync(userId);
        if (passenger == null)
            return null;


        Ticket ticket = new Ticket()
        {
            Description = ticketDto.Description,
            TicketPriority = ticketDto.TicketPriority,
            Subject = ticketDto.Subject,
            ReportTime = DateTime.UtcNow,
            TicketStatus = TicketStatus.Open,
            PassengerProfile = passenger
        };
        await _ticketRepository.CreateTicketAsync(ticket);
        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Ticket {TicketId} created by passenger {PassengerUserId} with priority {Priority}",
            ticket.Id,
            passenger.UserId,
            ticket.TicketPriority);

        CreateTicketResponseDto response = new CreateTicketResponseDto()
        {
            Id = ticket.Id,
            Description = ticket.Description,
            TicketPriority = ticket.TicketPriority,
            Subject = ticket.Subject,
            ReportTime = ticket.ReportTime,
            TicketStatus = ticket.TicketStatus
        };
        return response;
    }

    public async Task<GetAllTicketsResponseDto?> GetAllTicketsAsync(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        var passenger = await _passengerRepository.GetPassengerByIdAsync(userId);
        if (passenger == null)
            return null;

        List<Ticket> tickets = await _ticketRepository.GetAllTicketsAsync(userId);

        GetAllTicketsResponseDto response = new GetAllTicketsResponseDto();

        response.Tickets = tickets.Select(t => new CreateTicketResponseDto
        {
            Id = t.Id,
            Description = t.Description,
            TicketPriority = t.TicketPriority,
            Subject = t.Subject,
            ReportTime = t.ReportTime,
            TicketStatus = t.TicketStatus
        });

        return response;
    }

    public async Task<GetTicketByIdResponseDto?> GetTicketByIdAsync(ClaimsPrincipal principal, int id)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(sub, out var userId))
            return null;

        var passenger = await _passengerRepository.GetPassengerByIdAsync(userId);
        if (passenger == null)
            return null;

        var ticket = await _ticketRepository.GetTicketByIdAsync(id);
        if (ticket == null)
            return null;

        if (ticket.PassengerProfile.UserId != userId)
            throw new UnauthorizedAccessException();

        GetTicketByIdResponseDto response = new GetTicketByIdResponseDto()
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Description = ticket.Description,
            TicketPriority = ticket.TicketPriority,
            TicketStatus = ticket.TicketStatus,
            ReportTime = ticket.ReportTime
        };

        return response;
    }

    public async Task<List<AdminTicketResponseDto>> GetAdminTicketsAsync()
    {
        var tickets = await _ticketRepository.GetAllTicketsForAdminAsync();

        return tickets.Select(MapAdminTicket).ToList();
    }

    public async Task<AdminTicketResponseDto?> UpdateTicketStatusAsync(int id, UpdateTicketStatusRequestDto request)
    {
        var ticket = await _ticketRepository.GetTicketByIdAsync(id);
        if (ticket is null)
        {
            return null;
        }

        ticket.TicketStatus = request.TicketStatus;
        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketId} status updated to {TicketStatus}", ticket.Id, ticket.TicketStatus);

        ticket = await _ticketRepository.GetTicketByIdAsync(id);
        return ticket is null ? null : MapAdminTicket(ticket);
    }

    private static AdminTicketResponseDto MapAdminTicket(Ticket ticket)
    {
        return new AdminTicketResponseDto
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Description = ticket.Description,
            TicketPriority = ticket.TicketPriority,
            TicketStatus = ticket.TicketStatus,
            ReportTime = ticket.ReportTime,
            PassengerUserId = ticket.PassengerProfile.UserId,
            PassengerEmail = ticket.PassengerProfile.User.Email
        };
    }
}
