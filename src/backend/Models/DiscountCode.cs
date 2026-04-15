namespace Project.Models;

public class DiscountCode
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumRideValue { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsActive { get; set; }
}
