namespace Project.Services;

public interface IPriceService
{
    decimal EstimatePrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime);
    PriceQuoteBreakdown GetEstimatedBreakdown(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime);
}

public class PriceService : IPriceService
{
    private readonly decimal startingRate = 2.5m;
    private readonly decimal distancePricePerKilometer = 1.1m;
    private readonly decimal durationPricePerMinute = 0.3m;
    private readonly decimal vatMultiplier = 1.21m;

    public decimal EstimatePrice(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime)
    {
        return CalculatePriceCore(distance, duration, vehicleType, rideTime).Total;
    }

    public PriceQuoteBreakdown GetEstimatedBreakdown(decimal distance, decimal duration, VehicleType vehicleType, DateTime rideTime)
    {
        return CalculatePriceCore(distance, duration, vehicleType, rideTime);
    }

    private PriceQuoteBreakdown CalculatePriceCore(
        decimal distance,
        decimal duration,
        VehicleType vehicleType,
        DateTime rideTime)
    {
        decimal distanceCost = distance * distancePricePerKilometer;
        decimal durationCost = duration * durationPricePerMinute;
        decimal finalPrice = startingRate + distanceCost + durationCost;
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

        if (finalPrice < 5)
        {
            finalPrice = 5;
        }

        decimal vatAmount = Math.Round(finalPrice * (vatMultiplier - 1), 2, MidpointRounding.AwayFromZero);
        finalPrice *= vatMultiplier;
        finalPrice = Math.Round(finalPrice, 2, MidpointRounding.AwayFromZero);

        return new PriceQuoteBreakdown(
            StartingRate: startingRate,
            DistanceCost: Math.Round(distanceCost, 2, MidpointRounding.AwayFromZero),
            DurationCost: Math.Round(durationCost, 2, MidpointRounding.AwayFromZero),
            VehicleMultiplier: vehicleMultiplier,
            NightSurcharge: Math.Round(nightSurcharge, 2, MidpointRounding.AwayFromZero),
            VatAmount: vatAmount,
            Total: finalPrice,
            Distance: distance,
            Duration: duration,
            IsNightRateApplied: isNightRateApplied);
    }
}

public record PriceQuoteBreakdown(
    decimal StartingRate,
    decimal DistanceCost,
    decimal DurationCost,
    decimal VehicleMultiplier,
    decimal NightSurcharge,
    decimal VatAmount,
    decimal Total,
    decimal Distance,
    decimal Duration,
    bool IsNightRateApplied);
