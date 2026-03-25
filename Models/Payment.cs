using Project.Enums;

namespace Project.Models;

public class Payment
{
    public int Id {get;set;}
    public decimal PayAmount {get;set;}
    public Currency Currency {get;set;}
    public TransactionStatus TransactionStatus {get;set;}
    public string TransactionReference {get;set;}
    public DateTime PaymentDate {get;set;}
    public Ride Ride {get;set;}
}
