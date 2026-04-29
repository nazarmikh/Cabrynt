namespace Project.DTOs;

public class AdminTicketResponseDto
{
    public int Id { get; set; }
    public required string Subject { get; set; }
    public required string Description { get; set; }
    public TicketPriority TicketPriority { get; set; }
    public TicketStatus TicketStatus { get; set; }
    public DateTime ReportTime { get; set; }
    public int PassengerUserId { get; set; }
    public required string PassengerEmail { get; set; }
}
