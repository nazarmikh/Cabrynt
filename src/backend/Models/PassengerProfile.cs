using Project.Enums;

namespace Project.Models;

public class PassengerProfile
{

    public int UserId {get;set;}
    private string _name = null!;
    public required string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
        }
    }

    private string _homeAddress =null!;
    public string HomeAddress
    {
        get
        {
            return _homeAddress;
        }
        set
        {
            _homeAddress = value;
        }
    
    }

    private int _points = 0;
    public int Points
    {
        get
        {
            return _points;
        }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("Points cannot be negative");
            }
            _points = value;
        }
    }

    private PaymentMethod _preferredPaymentMethod = PaymentMethod.Card;
    public PaymentMethod PreferredPaymentMethod
    {
        get 
        {
            return _preferredPaymentMethod;
        }
        set
        {
            _preferredPaymentMethod = value;
        }
    }
    public User User {get;set;} = null!;
    public ICollection<Ticket> Tickets {get;set;} = new List<Ticket>();
    public ICollection<Ride> Rides {get;set;} = new List<Ride>();
}

