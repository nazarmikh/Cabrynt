namespace Project.Models;

public class Maintenance
{
    public int Id {get;set;}
    private DateTime _serviceDate;
    public DateTime ServiceDate
    {
        get
        {
            return _serviceDate;
        }
        set
        {
            if (value > DateTime.Today)
            {
                throw new ArgumentOutOfRangeException("Date cannot be in future");
            }
            _serviceDate = value;
        }
    }

    private string _description;
    public string Description
    {
        get
        {
            return _description;
        }
        set
        {
            _description = value;
        }
    }

    private string _technicianName;
    public string TechnicianName
    {
        get
        {
            return _technicianName;
        }
        set
        {
            _technicianName = value;
        }
    }

    private decimal _cost;
    public decimal Cost
    {
        get
        {
            return _cost;
        }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("Cost cannot be lower then 0");
            }
            _cost = value;
        }
    }

    private double _nextInspectionMileage;
    public double NextInspectionMileage
    {
        get {return _nextInspectionMileage;}
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException("Mileage cannot be <= 0");
            }
            _nextInspectionMileage = value;
        }
    }

    public Vehicle Vehicle {get;set;}
}

