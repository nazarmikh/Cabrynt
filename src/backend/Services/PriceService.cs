namespace Project.Services;

public interface IPriceService
{
    decimal EstimatePrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode);
    (decimal finalPrice, int loyaltyPointRecalculated) GetFinalPrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode);
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
            recalculateLoyaltyPoints: false).finalPrice;
    }


    public (decimal finalPrice, int loyaltyPointRecalculated) GetFinalPrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime, int loyaltyPoints, DiscountCode? discountCode)
    {
        return CalculatePriceCore(
            distance,
            duration,
            vehicleType,
            rideTime,
            loyaltyPoints,
            discountCode,
            recalculateLoyaltyPoints: true);
    }

    private (decimal finalPrice, int loyaltyPointRecalculated) CalculatePriceCore(
        decimal distance,
        decimal duration,
        VehicleType vehicleType,
        DateTime rideTime,
        int loyaltyPoints,
        DiscountCode? discountCode,
        bool recalculateLoyaltyPoints)
    {
        decimal finalPrice = startingRate + (distance * distancePricePerKilometer) + (duration * durationPricePerMinute);
        int loyaltyPointRecalculated = loyaltyPoints;

        if (vehicleType == VehicleType.Van)
        {
            finalPrice *= 1.5m;
        }
        else if (vehicleType == VehicleType.Luxury)
        {
            finalPrice *= 2.2m; 
        }

        TimeOnly rideTimeOnly = TimeOnly.FromDateTime(rideTime);

        if (rideTimeOnly >= new TimeOnly(22, 0) || rideTimeOnly <= new TimeOnly(6, 0))
        {
            finalPrice *= 1.15m;
        }

        var availablePointBlocks = loyaltyPoints / 100;
        var requestedLoyaltyDiscount = availablePointBlocks * discountPer100LoyaltyPoints;
        var maxLoyaltyDiscount = Math.Floor(finalPrice * 0.2m);
        var appliedLoyaltyDiscount = Math.Min(requestedLoyaltyDiscount, maxLoyaltyDiscount);

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
                finalPrice *= 1 - (discountCode.Value / 100m);
            }
            else if (discountCode.Type == DiscountType.Flat)
            {
                finalPrice -= discountCode.Value;
            }
        }


        // The current implementation enforces the minimum fare on the pre-VAT subtotal,
        // then applies VAT afterwards. That means the customer-facing minimum total is above 5.00 EUR.
        if (finalPrice < 5)
        {
            finalPrice = 5;
        }

        finalPrice *= vatMultiplier;
        // We round to 2 decimals with AwayFromZero so midpoint values use the
        // more typical financial rounding behavior, for example 19.965 -> 19.97.
        finalPrice = Math.Round(finalPrice, 2, MidpointRounding.AwayFromZero);

        return (finalPrice, loyaltyPointRecalculated);
    }
}
