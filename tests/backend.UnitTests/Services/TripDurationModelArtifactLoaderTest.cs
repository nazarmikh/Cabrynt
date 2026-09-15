using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Project.Services;

namespace backend.UnitTests.Services;

public class TripDurationModelArtifactLoaderTest : IDisposable
{
    private readonly string _cacheDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [Fact]
    public async Task GetVerifiedModelPathAsync_DownloadsAndCachesCompatibleReleaseAssets()
    {
        var modelBytes = await ReadTestModelAsync();
        var loader = CreateLoader(modelBytes, CreateMetadata(modelBytes));

        var result = await loader.GetVerifiedModelPathAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        Assert.Equal(modelBytes, await File.ReadAllBytesAsync(result));
        Assert.True(File.Exists(Path.Combine(_cacheDirectory, "trip-duration-residual.metadata.json")));
    }

    [Fact]
    public async Task GetVerifiedModelPathAsync_UsesValidCacheWhenReleaseRequestFails()
    {
        var modelBytes = await ReadTestModelAsync();
        var metadata = CreateMetadata(modelBytes);
        var firstLoader = CreateLoader(modelBytes, metadata);
        await firstLoader.GetVerifiedModelPathAsync(CancellationToken.None);

        var cachedLoader = CreateLoader(
            modelBytes,
            metadata,
            statusCode: HttpStatusCode.ServiceUnavailable);

        var result = await cachedLoader.GetVerifiedModelPathAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(File.Exists(result));
    }

    [Fact]
    public async Task GetVerifiedModelPathAsync_RejectsArtifactWithInvalidChecksum()
    {
        var modelBytes = await ReadTestModelAsync();
        var loader = CreateLoader(modelBytes, CreateMetadata(modelBytes, checksum: "invalid"));

        var result = await loader.GetVerifiedModelPathAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerifiedModelPathAsync_RejectsArtifactWithUnexpectedVersion()
    {
        var modelBytes = await ReadTestModelAsync();
        var loader = CreateLoader(modelBytes, CreateMetadata(modelBytes, version: "2.0.0"));

        var result = await loader.GetVerifiedModelPathAsync(CancellationToken.None);

        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }

    private TripDurationModelArtifactLoader CreateLoader(
        byte[] modelBytes,
        string metadata,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (statusCode != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(statusCode);
            }

            return request.RequestUri!.AbsolutePath.EndsWith(".onnx", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(modelBytes) }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(metadata) };
        });
        var client = new HttpClient(handler);
        var options = Options.Create(new TripDurationModelOptions
        {
            ModelArtifactUrl = "https://release.test/trip-duration-residual.onnx",
            MetadataUrl = "https://release.test/trip-duration-residual.metadata.json",
            ExpectedVersion = "1.0.0",
            CacheDirectory = _cacheDirectory
        });

        return new TripDurationModelArtifactLoader(
            client,
            options,
            NullLogger<TripDurationModelArtifactLoader>.Instance);
    }

    private static async Task<byte[]> ReadTestModelAsync() =>
        await File.ReadAllBytesAsync(Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "sum-23-features.onnx"));

    private static string CreateMetadata(byte[] modelBytes, string? checksum = null, string version = "1.0.0") =>
        JsonSerializer.Serialize(new
        {
            model_version = version,
            model_sha256 = checksum ?? Convert.ToHexString(SHA256.HashData(modelBytes)),
            input = new
            {
                name = TripDurationModelContract.InputName,
                data_type = "float32",
                feature_columns = TripDurationModelContract.FeatureColumns
            },
            output = new
            {
                name = TripDurationModelContract.OutputName,
                meaning = "residual_correction_minutes"
            }
        });

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(handler(request));
    }
}
