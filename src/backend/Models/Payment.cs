using Project.Enums;

namespace Project.Models;

public class Payment
{
    public int Id {get;set;}
    private decimal _payAmount {get;set;}
    public decimal PayAmount 
    { 
        get {return _payAmount;}
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("Pay amount cannot be lower then 0");
            }
            _payAmount = value;
        }
    }

    public Currency Currency {get;set;}
    public TransactionStatus TransactionStatus {get;set;}
    public string? TransactionReference {get;set;}
    public DateTime PaymentDate {get;set;}
    public required Ride Ride {get;set;}
}
