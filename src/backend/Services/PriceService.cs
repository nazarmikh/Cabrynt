namespace Project.Services;

public interface IPriceService
{
    decimal EstimatePrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode);
    (decimal finalPrice, int loyaltyPointRecalculated) GetFinalPrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode);
    PriceQuoteBreakdown GetEstimatedBreakdown(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode);
}

public class PriceService : IPriceService
{
    private readonly decimal startingRate = 2.5m;
    private readonly decimal distancePricePerKilometer = 1.1m;
    private readonly decimal durationPricePerMinute = 0.3m;
    private readonly decimal discountPer100LoyaltyPoints = 1;
    private readonly decimal vatMultiplier = 1.21m;

    public decimal EstimatePrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode)
    {
        return CalculatePriceCore(
            distance,
            duration,
            vehicleType,
            rideTime,
            loyaltyPoints,
            discountCode,
            recalculateLoyaltyPoints: false).Total;
    }


    public (decimal finalPrice, int loyaltyPointRecalculated) GetFinalPrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode)
    {
        var result = CalculatePriceCore(
            distance,
            duration,
            vehicleType,
            rideTime,
            loyaltyPoints,
            discountCode,
            recalculateLoyaltyPoints: true);

        return (result.Total, result.LoyaltyPointRecalculated);
    }

    public PriceQuoteBreakdown GetEstimatedBreakdown(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode)
    {
        return CalculatePriceCore(
            distance,
            duration,
            vehicleType,
            rideTime,
            loyaltyPoints,
            discountCode,
            recalculateLoyaltyPoints: false);
    }

    private PriceQuoteBreakdown CalculatePriceCore(
        decimal distance,
        decimal duration,
        VehicleType vehicleType,
        DateTime rideTime,
        int loyaltyPoints,
        DiscountCode? discountCode,
        bool recalculateLoyaltyPoints)
    {
        decimal distanceCost = distance * distancePricePerKilometer;
        decimal durationCost = duration * durationPricePerMinute;
        decimal finalPrice = startingRate + distanceCost + durationCost;
        int loyaltyPointRecalculated = loyaltyPoints;
        decimal vehicleMultiplier = 1m;

        if (vehicleType == VehicleType.Van)
        {
            vehicleMultiplier = 1.5m;
        }
        else if (vehicleType == VehicleType.Luxury)
        {
            vehicleMultiplier = 2.2m;
        }

        finalPrice *= vehicleMultiplier;

        TimeOnly rideTimeOnly = TimeOnly.FromDateTime(rideTime);
        decimal nightSurcharge = 0m;
        bool isNightRateApplied = false;

        if (rideTimeOnly >= new TimeOnly(22, 0) || rideTimeOnly <= new TimeOnly(6, 0))
        {
            isNightRateApplied = true;
            nightSurcharge = finalPrice * 0.15m;
            finalPrice += nightSurcharge;
        }

        var availablePointBlocks = loyaltyPoints / 100;
        var requestedLoyaltyDiscount = availablePointBlocks * discountPer100LoyaltyPoints;
        var maxLoyaltyDiscount = Math.Floor(finalPrice * 0.2m);
        var appliedLoyaltyDiscount = Math.Min(requestedLoyaltyDiscount, maxLoyaltyDiscount);
        decimal codeDiscount = 0m;

        if (appliedLoyaltyDiscount >= 1)
        {
            finalPrice -= appliedLoyaltyDiscount;
            if (recalculateLoyaltyPoints)
            {
                loyaltyPointRecalculated = loyaltyPoints - (int)(appliedLoyaltyDiscount * 100);
            }
        }

        if (discountCode is not null
            && discountCode.IsActive
            && discountCode.ExpirationDate > rideTime
            && finalPrice >= discountCode.MinimumRideValue)
        {
            if (discountCode.Type == DiscountType.Percentage)
            {
                codeDiscount = finalPrice * (discountCode.Value / 100m);
                finalPrice -= codeDiscount;
            }
            else if (discountCode.Type == DiscountType.Flat)
            {
                codeDiscount = discountCode.Value;
                finalPrice -= codeDiscount;
            }
        }


        // The current implementation enforces the minimum fare on the pre-VAT subtotal,
        // then applies VAT afterwards. That means the customer-facing minimum total is above 5.00 EUR.
        if (finalPrice < 5)
        {
            finalPrice = 5;
        }

        decimal vatAmount = Math.Round(finalPrice * (vatMultiplier - 1), 2, MidpointRounding.AwayFromZero);
        finalPrice *= vatMultiplier;
        // We round to 2 decimals with AwayFromZero so midpoint values use the
        // more typical financial rounding behavior, for example 19.965 -> 19.97.
        finalPrice = Math.Round(finalPrice, 2, MidpointRounding.AwayFromZero);

        return new PriceQuoteBreakdown(
            StartingRate: startingRate,
            DistanceCost: Math.Round(distanceCost, 2, MidpointRounding.AwayFromZero),
            DurationCost: Math.Round(durationCost, 2, MidpointRounding.AwayFromZero),
            VehicleMultiplier: vehicleMultiplier,
            NightSurcharge: Math.Round(nightSurcharge, 2, MidpointRounding.AwayFromZero),
            LoyaltyDiscount: Math.Round(appliedLoyaltyDiscount, 2, MidpointRounding.AwayFromZero),
            CodeDiscount: Math.Round(codeDiscount, 2, MidpointRounding.AwayFromZero),
            VatAmount: vatAmount,
            Total: finalPrice,
            Distance: distance,
            Duration: duration,
            IsNightRateApplied: isNightRateApplied,
            LoyaltyPointRecalculated: loyaltyPointRecalculated);
    }
}

public record PriceQuoteBreakdown(
    decimal StartingRate,
    decimal DistanceCost,
    decimal DurationCost,
    decimal VehicleMultiplier,
    decimal NightSurcharge,
    decimal LoyaltyDiscount,
    decimal CodeDiscount,
    decimal VatAmount,
    decimal Total,
    decimal Distance,
    decimal Duration,
    bool IsNightRateApplied,
    int LoyaltyPointRecalculated);
