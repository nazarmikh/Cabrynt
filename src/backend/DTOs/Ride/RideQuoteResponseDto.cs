namespace Project.DTOs;

public class RideQuoteResponseDto
{
    public decimal Distance { get; set; }
    public decimal Duration { get; set; }
    public decimal BaseFare { get; set; }
    public decimal DistanceCost { get; set; }
    public decimal DurationCost { get; set; }
    public decimal VehicleMultiplier { get; set; }
    public decimal NightSurcharge { get; set; }
    public bool IsNightRateApplied { get; set; }
    public decimal LoyaltyDiscount { get; set; }
    public decimal CodeDiscount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal EstimatedPrice { get; set; }
}
