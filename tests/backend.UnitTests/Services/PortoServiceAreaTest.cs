using Project.Services;

namespace backend.UnitTests.Services;

public sealed class PortoServiceAreaTest
{
    [Fact]
    public void Contains_ReturnsTrue_ForCoordinatesOnServiceAreaBoundary()
    {
        var isInside = PortoServiceArea.Contains(
            PortoServiceArea.MaximumLatitude,
            PortoServiceArea.MinimumLongitude);

        Assert.True(isInside);
    }

    [Fact]
    public void Contains_ReturnsFalse_ForCoordinatesOutsideServiceArea()
    {
        var isInside = PortoServiceArea.Contains(41.2356, -8.6783);

        Assert.False(isInside);
    }
}
