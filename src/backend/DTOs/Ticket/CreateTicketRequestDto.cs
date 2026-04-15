namespace Project.DTOs;

public class CreateTicketRequestDto
{
    public required string Subject {get;set;}
    public required string Description {get;set;}
    public TicketPriority TicketPriority {get;set;}
}