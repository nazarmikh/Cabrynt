namespace Project.Models;

public class Vehicle
{
    public int Id { get; set; }
    private string _vin = null!;
    public required string VIN
    {
        get
        {
            return _vin;
        }
        set
        {
            _vin = value;
        }
    }

    private string _licencePlate = null!;
    public required string LicencePlate
    {
        get
        {
            return _licencePlate;
        }
        set
        {
            _licencePlate = value;
        }
    }

    private string _model = null!;
    public string Model
    {
        get
        {
            return _model;
        }
        set
        {
            _model = value;
        }
    }

    public VehicleType VehicleType { get; set; }

    private int _year;
    public int Year
    {
        get
        {
            return _year;
        }
        set
        {
            if (value < 1970 || value > DateTime.Now.Year)
            {
                throw new ArgumentOutOfRangeException("Manufacture year cannot be lower then 1990");
            }
            _year = value;
        }
    }

    public VehicleStatus VehicleStatus { get; set; }
    public User? User { get; set; }
    public int? UserId { get; set; }
}
