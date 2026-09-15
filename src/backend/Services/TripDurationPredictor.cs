using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Project.Services;

public interface ITripDurationPredictor
{
    bool IsAvailable { get; }

    float? PredictResidualMinutes(IReadOnlyList<float> features);
}

public sealed class TripDurationModelOptions
{
    public const string SectionName = "TripDurationModel";

    public bool Enabled { get; set; }
    public string ModelPath { get; set; } = string.Empty;
    public string ModelArtifactUrl { get; set; } = string.Empty;
    public string MetadataUrl { get; set; } = string.Empty;
    public string ExpectedVersion { get; set; } = string.Empty;
    public string CacheDirectory { get; set; } = "App_Data/models";
    public int DownloadTimeoutSeconds { get; set; } = 15;
}

public static class TripDurationModelContract
{
    public const string InputName = "features";
    public const string OutputName = "variable";
    public const int FeatureCount = 23;

    public static IReadOnlyList<string> FeatureColumns { get; } = Array.AsReadOnly(
    [
        "pickup_longitude",
        "pickup_latitude",
        "destination_longitude",
        "destination_latitude",
        "straight_line_km",
        "hour",
        "weekday",
        "month",
        "is_weekend",
        "is_public_holiday",
        "hour_sin",
        "hour_cos",
        "weekday_sin",
        "weekday_cos",
        "month_sin",
        "month_cos",
        "temperature_c",
        "precipitation_mm",
        "cloud_cover_percent",
        "wind_speed_kmh",
        "is_precipitating",
        "osrm_distance_km",
        "osrm_duration_minutes"
    ]);
}

public sealed class DisabledTripDurationPredictor : ITripDurationPredictor
{
    public bool IsAvailable => false;

    public float? PredictResidualMinutes(IReadOnlyList<float> features)
    {
        return null;
    }
}

public sealed class OnnxTripDurationPredictor : ITripDurationPredictor, IDisposable
{
    private readonly InferenceSession _session;
    private readonly ILogger<OnnxTripDurationPredictor> _logger;

    public OnnxTripDurationPredictor(
        IOptions<TripDurationModelOptions> options,
        ILogger<OnnxTripDurationPredictor> logger)
    {
        var modelOptions = options.Value;
        if (string.IsNullOrWhiteSpace(modelOptions.ModelPath))
        {
            throw new InvalidOperationException(
                "Trip duration model is enabled but 'TripDurationModel:ModelPath' is empty.");
        }

        var modelPath = System.IO.Path.GetFullPath(modelOptions.ModelPath);
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "Trip duration model is enabled but the ONNX file was not found.",
                modelPath);
        }

        _session = new InferenceSession(modelPath);
        _logger = logger;
        ValidateModelContract();

        _logger.LogInformation("Loaded trip duration ONNX model from {ModelPath}", modelPath);
    }

    public bool IsAvailable => true;

    public float? PredictResidualMinutes(IReadOnlyList<float> features)
    {
        ValidateFeatures(features);

        var inputTensor = new DenseTensor<float>(
            features.ToArray(),
            [1, TripDurationModelContract.FeatureCount]);
        var input = NamedOnnxValue.CreateFromTensor(TripDurationModelContract.InputName, inputTensor);

        using var results = _session.Run([input]);
        var output = results.Single(result => result.Name == TripDurationModelContract.OutputName)
            .AsTensor<float>()
            .ToArray();

        if (output.Length != 1 || !float.IsFinite(output[0]))
        {
            throw new InvalidOperationException(
                "Trip duration ONNX model returned an invalid residual prediction.");
        }

        return output[0];
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private void ValidateModelContract()
    {
        if (!_session.InputMetadata.TryGetValue(TripDurationModelContract.InputName, out var input)
            || input.ElementType != typeof(float)
            || input.Dimensions.Length != 2
            || input.Dimensions[1] != TripDurationModelContract.FeatureCount)
        {
            throw new InvalidOperationException(
                "Trip duration ONNX model does not match the expected float32 feature input contract.");
        }

        if (!_session.OutputMetadata.TryGetValue(TripDurationModelContract.OutputName, out var output)
            || output.ElementType != typeof(float)
            || output.Dimensions.Length != 2
            || output.Dimensions[1] != 1)
        {
            throw new InvalidOperationException(
                "Trip duration ONNX model does not match the expected residual output contract.");
        }
    }

    private static void ValidateFeatures(IReadOnlyList<float> features)
    {
        if (features.Count != TripDurationModelContract.FeatureCount)
        {
            throw new ArgumentException(
                $"Trip duration prediction requires {TripDurationModelContract.FeatureCount} features.",
                nameof(features));
        }

        if (features.Any(value => !float.IsFinite(value)))
        {
            throw new ArgumentException(
                "Trip duration prediction features must be finite values.",
                nameof(features));
        }
    }
}
