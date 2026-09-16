using Project.Enums;
using Project.Services;

namespace backend.UnitTests.Services;

public class PriceServiceTest
{
    private readonly PriceService _service = new();

    [Fact]
    public void EstimatePrice_UsesMinimumFareBeforeVat()
    {
        var price = _service.EstimatePrice(0m, 0m, VehicleType.Standard, new DateTime(2026, 9, 16, 12, 0, 0));

        Assert.Equal(6.05m, price);
    }

    [Fact]
    public void GetEstimatedBreakdown_AppliesVehicleMultiplier()
    {
        var standard = _service.GetEstimatedBreakdown(10m, 20m, VehicleType.Standard, new DateTime(2026, 9, 16, 12, 0, 0));
        var luxury = _service.GetEstimatedBreakdown(10m, 20m, VehicleType.Luxury, new DateTime(2026, 9, 16, 12, 0, 0));

        Assert.Equal(1m, standard.VehicleMultiplier);
        Assert.Equal(2.2m, luxury.VehicleMultiplier);
        Assert.True(luxury.Total > standard.Total);
    }

    [Fact]
    public void GetEstimatedBreakdown_AppliesNightSurcharge()
    {
        var result = _service.GetEstimatedBreakdown(10m, 20m, VehicleType.Standard, new DateTime(2026, 9, 16, 23, 0, 0));

        Assert.True(result.IsNightRateApplied);
        Assert.True(result.NightSurcharge > 0m);
    }
}
