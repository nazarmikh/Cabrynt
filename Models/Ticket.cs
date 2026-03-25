namespace Project.Models;
using Project.Enums;

public class Ticket
{
    public int Id {get;set;}
    public string Subject {get;set;}
    public string Description {get;set;}
    public TicketPriority TicketPriority {get;set;}
    public TicketStatus TicketStatus {get;set;}
    public DateTime ReportTime {get;set;}
    public Passenger Passenger {get;set;}
}

