using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Path = System.IO.Path;

namespace Project.Services;

public sealed class TripDurationModelArtifactLoader
{
    private const string ModelFileName = "trip-duration-residual.onnx";
    private const string MetadataFileName = "trip-duration-residual.metadata.json";
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly TripDurationModelOptions _options;
    private readonly ILogger<TripDurationModelArtifactLoader> _logger;

    public TripDurationModelArtifactLoader(
        HttpClient httpClient,
        IOptions<TripDurationModelOptions> options,
        ILogger<TripDurationModelArtifactLoader> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> GetVerifiedModelPathAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ModelPath))
        {
            var modelPath = Path.GetFullPath(_options.ModelPath);
            return File.Exists(modelPath) ? modelPath : null;
        }

        if (HasReleaseUrls())
        {
            try
            {
                return await DownloadAndCacheAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException
                or IOException
                or JsonException
                or InvalidOperationException)
            {
                _logger.LogWarning(
                    exception,
                    "Could not download a verified trip duration model; checking the local cache.");
            }
        }
        else
        {
            _logger.LogWarning(
                "Trip duration model is enabled but no local model path or release asset URLs are configured.");
        }

        return await GetCachedModelPathAsync(cancellationToken);
    }

    private bool HasReleaseUrls() =>
        Uri.TryCreate(_options.ModelArtifactUrl, UriKind.Absolute, out var modelUri)
        && Uri.TryCreate(_options.MetadataUrl, UriKind.Absolute, out var metadataUri)
        && modelUri.Scheme == Uri.UriSchemeHttps
        && metadataUri.Scheme == Uri.UriSchemeHttps;

    private async Task<string> DownloadAndCacheAsync(CancellationToken cancellationToken)
    {
        var modelBytes = await _httpClient.GetByteArrayAsync(_options.ModelArtifactUrl, cancellationToken);
        var metadataJson = await _httpClient.GetStringAsync(_options.MetadataUrl, cancellationToken);
        ValidateArtifact(modelBytes, metadataJson);

        var paths = GetCachePaths();
        Directory.CreateDirectory(paths.Directory);
        await WriteAtomicallyAsync(paths.ModelPath, modelBytes, cancellationToken);
        await WriteAtomicallyAsync(paths.MetadataPath, metadataJson, cancellationToken);

        _logger.LogInformation(
            "Downloaded and verified trip duration model version {ModelVersion}.",
            _options.ExpectedVersion);

        return paths.ModelPath;
    }

    private async Task<string?> GetCachedModelPathAsync(CancellationToken cancellationToken)
    {
        var paths = GetCachePaths();
        if (!File.Exists(paths.ModelPath) || !File.Exists(paths.MetadataPath))
        {
            return null;
        }

        try
        {
            var modelBytes = await File.ReadAllBytesAsync(paths.ModelPath, cancellationToken);
            var metadataJson = await File.ReadAllTextAsync(paths.MetadataPath, cancellationToken);
            ValidateArtifact(modelBytes, metadataJson);
            _logger.LogInformation(
                "Using verified cached trip duration model version {ModelVersion}.",
                _options.ExpectedVersion);
            return paths.ModelPath;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "The cached trip duration model is not valid.");
            return null;
        }
    }

    private void ValidateArtifact(byte[] modelBytes, string metadataJson)
    {
        var metadata = JsonSerializer.Deserialize<TripDurationModelMetadata>(
            metadataJson,
            MetadataJsonOptions)
            ?? throw new InvalidOperationException("Trip duration model metadata is empty.");

        if (!string.Equals(metadata.ModelVersion, _options.ExpectedVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Trip duration model metadata does not match the configured model version.");
        }

        var checksum = Convert.ToHexString(SHA256.HashData(modelBytes));
        if (!string.Equals(checksum, metadata.ModelSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Trip duration model checksum verification failed.");
        }

        if (metadata.Input is null
            || metadata.Output is null
            || !string.Equals(metadata.Input.Name, TripDurationModelContract.InputName, StringComparison.Ordinal)
            || !string.Equals(metadata.Input.DataType, "float32", StringComparison.Ordinal)
            || metadata.Input.FeatureColumns is null
            || !metadata.Input.FeatureColumns.SequenceEqual(TripDurationModelContract.FeatureColumns)
            || !string.Equals(metadata.Output.Name, TripDurationModelContract.OutputName, StringComparison.Ordinal)
            || !string.Equals(metadata.Output.Meaning, "residual_correction_minutes", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Trip duration model metadata does not match the inference contract.");
        }
    }

    private (string Directory, string ModelPath, string MetadataPath) GetCachePaths()
    {
        var directory = Path.GetFullPath(_options.CacheDirectory);
        return (
            directory,
            Path.Combine(directory, ModelFileName),
            Path.Combine(directory, MetadataFileName));
    }

    private static async Task WriteAtomicallyAsync(
        string destinationPath,
        byte[] contents,
        CancellationToken cancellationToken)
    {
        var temporaryPath = destinationPath + ".tmp";
        await File.WriteAllBytesAsync(temporaryPath, contents, cancellationToken);
        File.Move(temporaryPath, destinationPath, overwrite: true);
    }

    private static Task WriteAtomicallyAsync(
        string destinationPath,
        string contents,
        CancellationToken cancellationToken) =>
        WriteAtomicallyAsync(destinationPath, System.Text.Encoding.UTF8.GetBytes(contents), cancellationToken);

    private sealed class TripDurationModelMetadata
    {
        public string ModelVersion { get; set; } = string.Empty;
        public string ModelSha256 { get; set; } = string.Empty;
        public ModelInputMetadata? Input { get; set; }
        public ModelOutputMetadata? Output { get; set; }
    }

    private sealed class ModelInputMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public List<string>? FeatureColumns { get; set; }
    }

    private sealed class ModelOutputMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
    }
}

public sealed class TripDurationPredictorProvider : ITripDurationPredictor, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private ITripDurationPredictor _predictor = new DisabledTripDurationPredictor();

    public TripDurationPredictorProvider(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public bool IsAvailable => Volatile.Read(ref _predictor).IsAvailable;

    public float? PredictResidualMinutes(IReadOnlyList<float> features) =>
        Volatile.Read(ref _predictor).PredictResidualMinutes(features);

    public void Load(string modelPath)
    {
        var replacement = new OnnxTripDurationPredictor(
            Options.Create(new TripDurationModelOptions { ModelPath = modelPath }),
            _loggerFactory.CreateLogger<OnnxTripDurationPredictor>());
        var previous = Interlocked.Exchange(ref _predictor, replacement);

        if (previous is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _predictor, new DisabledTripDurationPredictor()) is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

public sealed class TripDurationModelInitializationService : IHostedService
{
    private readonly TripDurationModelArtifactLoader _artifactLoader;
    private readonly TripDurationPredictorProvider _predictorProvider;
    private readonly TripDurationModelStatusProvider _statusProvider;
    private readonly TripDurationModelOptions _options;
    private readonly ILogger<TripDurationModelInitializationService> _logger;

    public TripDurationModelInitializationService(
        TripDurationModelArtifactLoader artifactLoader,
        TripDurationPredictorProvider predictorProvider,
        TripDurationModelStatusProvider statusProvider,
        IOptions<TripDurationModelOptions> options,
        ILogger<TripDurationModelInitializationService> logger)
    {
        _artifactLoader = artifactLoader;
        _predictorProvider = predictorProvider;
        _statusProvider = statusProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var modelPath = await _artifactLoader.GetVerifiedModelPathAsync(cancellationToken);
        if (modelPath is null)
        {
            _statusProvider.SetUnavailable(_options.ExpectedVersion);
            _logger.LogWarning("Trip duration model is unavailable; quote estimates will use OSRM.");
            return;
        }

        try
        {
            _predictorProvider.Load(modelPath);
            _statusProvider.SetReady(_options.ExpectedVersion);
        }
        catch (Exception exception)
        {
            _statusProvider.SetUnavailable(_options.ExpectedVersion);
            _logger.LogError(exception, "Trip duration model could not be initialized; quote estimates will use OSRM.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
