namespace Project.Models;

public class Passenger : User
{
    private string _name;
    public string Name
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

    private string _homeAddress;
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

    private int _points;
    public int Points
    {
        get
        {
            return _points;
        }
        set
        {
            _points = value;
        }
    }

    private string _preferredPaymentMethod;
    public string PreferredPaymentMethod
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

    public ICollection<Ticket> tickets {get;set;} = new List<Ticket>();
}

