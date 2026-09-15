using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Project.Services;

namespace backend.UnitTests.Services;

public class OnnxTripDurationPredictorTest
{
    [Fact]
    public void PredictResidualMinutes_RunsTheConfiguredOnnxModel()
    {
        using var predictor = CreatePredictor();
        var features = Enumerable.Repeat(1f, TripDurationModelContract.FeatureCount).ToArray();

        var result = predictor.PredictResidualMinutes(features);

        Assert.Equal(23f, result);
    }

    [Fact]
    public void PredictResidualMinutes_RejectsIncorrectFeatureCount()
    {
        using var predictor = CreatePredictor();

        var exception = Assert.Throws<ArgumentException>(
            () => predictor.PredictResidualMinutes([1f]));

        Assert.Contains("23 features", exception.Message);
    }

    [Fact]
    public void DisabledPredictor_ReturnsNoPrediction()
    {
        ITripDurationPredictor predictor = new DisabledTripDurationPredictor();

        var result = predictor.PredictResidualMinutes([]);

        Assert.False(predictor.IsAvailable);
        Assert.Null(result);
    }

    [Fact]
    public void FeatureContract_DefinesTheExpectedOnnxInputOrder()
    {
        Assert.Equal(TripDurationModelContract.FeatureCount, TripDurationModelContract.FeatureColumns.Count);
        Assert.Equal("pickup_longitude", TripDurationModelContract.FeatureColumns[0]);
        Assert.Equal("osrm_duration_minutes", TripDurationModelContract.FeatureColumns[^1]);
    }

    private static OnnxTripDurationPredictor CreatePredictor()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sum-23-features.onnx");

        return new OnnxTripDurationPredictor(
            Options.Create(new TripDurationModelOptions { ModelPath = modelPath }),
            NullLogger<OnnxTripDurationPredictor>.Instance);
    }
}
