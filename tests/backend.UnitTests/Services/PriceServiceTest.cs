using Project.Enums;
using Project.Models;
using Project.Services;

namespace backend.UnitTests.Services;

public class PriceServiceTest
{
    private readonly PriceService _service = new();

    [Fact]
    public void EstimatePrice_ReturnsExpectedTotal_ForStandardDaytimeRide()
    {
        var result = _service.EstimatePrice(
            distance: 10m,
            duration: 20m,
            vehicleType: VehicleType.Standard,
            rideTime: new DateTime(2026, 4, 15, 14, 0, 0, DateTimeKind.Utc),
            loyaltyPoints: 0,
            discountCode: null);

        Assert.Equal(23.60m, result);
    }

    [Fact]
    public void EstimatePrice_AppliesVehicleMultiplierNightSurchargeAndPercentageDiscount()
    {
        var discountCode = new DiscountCode
        {
            Code = "SUMMER15",
            Type = DiscountType.Percentage,
            Value = 15m,
            MinimumRideValue = 10m,
            ExpirationDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        var result = _service.EstimatePrice(
            distance: 10m,
            duration: 20m,
            vehicleType: VehicleType.Luxury,
            rideTime: new DateTime(2026, 4, 15, 22, 0, 0, DateTimeKind.Utc),
            loyaltyPoints: 0,
            discountCode: discountCode);

        Assert.Equal(50.74m, result);
    }

    [Fact]
    public void GetFinalPrice_AppliesLoyaltyCapAndReturnsRemainingPoints()
    {
        var result = _service.GetFinalPrice(
            distance: 10m,
            duration: 20m,
            vehicleType: VehicleType.Standard,
            rideTime: new DateTime(2026, 4, 15, 14, 0, 0, DateTimeKind.Utc),
            loyaltyPoints: 1000,
            discountCode: null);

        Assert.Equal(19.97m, result.finalPrice);
        Assert.Equal(700, result.loyaltyPointRecalculated);
    }

    [Fact]
    public void EstimatePrice_AppliesFlatDiscount_WhenCodeIsValid()
    {
        var discountCode = new DiscountCode
        {
            Code = "WELCOME5",
            Type = DiscountType.Flat,
            Value = 5m,
            MinimumRideValue = 10m,
            ExpirationDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        var result = _service.EstimatePrice(
            distance: 10m,
            duration: 20m,
            vehicleType: VehicleType.Standard,
            rideTime: new DateTime(2026, 4, 15, 14, 0, 0, DateTimeKind.Utc),
            loyaltyPoints: 0,
            discountCode: discountCode);

        Assert.Equal(17.55m, result);
    }

    [Fact]
    public void EstimatePrice_IgnoresDiscountCode_WhenItIsExpired()
    {
        var expiredCode = new DiscountCode
        {
            Code = "OLD15",
            Type = DiscountType.Percentage,
            Value = 15m,
            MinimumRideValue = 10m,
            ExpirationDate = new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        var result = _service.EstimatePrice(
            distance: 10m,
            duration: 20m,
            vehicleType: VehicleType.Standard,
            rideTime: new DateTime(2026, 4, 15, 14, 0, 0, DateTimeKind.Utc),
            loyaltyPoints: 0,
            discountCode: expiredCode);

        Assert.Equal(23.60m, result);
    }

    [Fact]
    public void EstimatePrice_EnforcesMinimumFareAfterDiscounts()
    {
        var discountCode = new DiscountCode
        {
            Code = "FREE10",
            Type = DiscountType.Flat,
            Value = 10m,
            MinimumRideValue = 0m,
            ExpirationDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        var result = _service.EstimatePrice(
            distance: 1m,
            duration: 1m,
            vehicleType: VehicleType.Standard,
            rideTime: new DateTime(2026, 4, 15, 14, 0, 0, DateTimeKind.Utc),
            loyaltyPoints: 0,
            discountCode: discountCode);

        Assert.Equal(6.05m, result);
    }

    [Fact]
    public void EstimatePrice_AppliesNightSurchargeAtSixAmBoundary()
    {
        var result = _service.EstimatePrice(
            distance: 10m,
            duration: 20m,
            vehicleType: VehicleType.Standard,
            rideTime: new DateTime(2026, 4, 15, 6, 0, 0, DateTimeKind.Utc),
            loyaltyPoints: 0,
            discountCode: null);

        Assert.Equal(27.13m, result);
    }

    [Fact]
    public void EstimatePrice_DoesNotApplyNightSurchargeAfterSixAmBoundary()
    {
        var result = _service.EstimatePrice(
            distance: 10m,
            duration: 20m,
            vehicleType: VehicleType.Standard,
            rideTime: new DateTime(2026, 4, 15, 6, 1, 0, DateTimeKind.Utc),
            loyaltyPoints: 0,
            discountCode: null);

        Assert.Equal(23.60m, result);
    }
}
