using Project.Services;

namespace backend.UnitTests.Services;

public class TripDurationFeatureBuilderTest
{
    private readonly TripDurationFeatureBuilder _builder = new();

    [Fact]
    public void Build_ProducesTheOnnxContractFeaturesInOrder()
    {
        var features = _builder.Build(CreateContext(
            quoteRequestedAt: new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero),
            isPublicHoliday: true));

        Assert.Equal(TripDurationModelContract.FeatureCount, features.Length);
        Assert.Equal(-8.6291f, features[0]);
        Assert.Equal(41.1579f, features[1]);
        Assert.Equal(-8.65f, features[2]);
        Assert.Equal(41.17f, features[3]);
        Assert.InRange(features[4], 2.2070f, 2.2072f);
        Assert.Equal(14f, features[5]);
        Assert.Equal(0f, features[6]);
        Assert.Equal(1f, features[7]);
        Assert.Equal(0f, features[8]);
        Assert.Equal(1f, features[9]);
        Assert.InRange(features[10], -0.5001f, -0.4999f);
        Assert.InRange(features[11], -0.8661f, -0.8660f);
        Assert.Equal(0f, features[12]);
        Assert.Equal(1f, features[13]);
        Assert.InRange(features[14], 0.4999f, 0.5001f);
        Assert.InRange(features[15], 0.8660f, 0.8661f);
        Assert.Equal(18.5f, features[16]);
        Assert.Equal(0.3f, features[17]);
        Assert.Equal(73f, features[18]);
        Assert.Equal(14.2f, features[19]);
        Assert.Equal(1f, features[20]);
        Assert.Equal(4.2f, features[21]);
        Assert.Equal(11.5f, features[22]);
    }

    [Fact]
    public void Build_UsesPortoLocalTimeForCalendarFeatures()
    {
        var features = _builder.Build(CreateContext(
            quoteRequestedAt: new DateTimeOffset(2024, 7, 1, 14, 30, 0, TimeSpan.Zero),
            isPublicHoliday: false));

        Assert.Equal(15f, features[5]);
        Assert.Equal(0f, features[6]);
        Assert.Equal(7f, features[7]);
        Assert.Equal(0f, features[9]);
    }

    [Fact]
    public void Build_RejectsStraightLineFallbackRoutes()
    {
        var context = CreateContext() with
        {
            RouteEstimate = new RouteEstimate(4.2, 11.5, RouteEstimateSource.StraightLineFallback)
        };

        var exception = Assert.Throws<InvalidOperationException>(() => _builder.Build(context));

        Assert.Equal("Trip duration features require an OSRM route estimate.", exception.Message);
    }

    private static TripDurationFeatureContext CreateContext(
        DateTimeOffset? quoteRequestedAt = null,
        bool isPublicHoliday = false)
    {
        return new TripDurationFeatureContext(
            new RouteRequest(41.1579, -8.6291, 41.17, -8.65),
            new RouteEstimate(4.2, 11.5, RouteEstimateSource.Osrm),
            quoteRequestedAt ?? new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero),
            new WeatherSnapshot(18.5, 0.3, 73, 14.2),
            isPublicHoliday);
    }
}
