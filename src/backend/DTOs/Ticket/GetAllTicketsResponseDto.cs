namespace Project.DTOs;

public class GetAllTicketsResponseDto
{
    public IEnumerable<CreateTicketResponseDto> Tickets { get; set; } = new List<CreateTicketResponseDto>();
}