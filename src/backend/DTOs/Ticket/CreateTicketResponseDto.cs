namespace Project.DTOs;

public class CreateTicketResponseDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority TicketPriority { get; set; }
    public TicketStatus TicketStatus { get; set; }
    public DateTime ReportTime { get; set; }
}