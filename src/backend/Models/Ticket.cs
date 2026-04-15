namespace Project.Models;
using Project.Enums;

public class Ticket
{
    public int Id {get;set;}
    public required string Subject {get;set;}
    public required string Description {get;set;}
    public TicketPriority TicketPriority {get;set;}
    public TicketStatus TicketStatus {get;set;}
    public DateTime ReportTime {get;set;}
    public required PassengerProfile PassengerProfile {get;set;}
}

