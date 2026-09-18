using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Project.Services;

namespace backend.UnitTests.Services;

public class DeploymentConfigurationTest
{
    [Fact]
    public void GetConfiguredOrigins_SplitsCommaSeparatedEnvironmentValue()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins"] = "https://app.cabrynt.example, https://www.cabrynt.example/"
        });

        var origins = CorsOriginConfiguration.GetConfiguredOrigins(configuration);

        Assert.Equal(["https://app.cabrynt.example", "https://www.cabrynt.example"], origins);
    }

    [Fact]
    public void Validate_RejectsLocalhostOriginInProduction()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=db;Database=cabrynt;Username=cabrynt;Password=secret",
            ["Admin:Password"] = "secret",
            ["DataProtection:ApplicationName"] = "Cabrynt",
            ["DataProtection:KeyDirectory"] = "/keys",
            ["Cors:AllowedOrigins"] = "http://localhost:3000"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(configuration, new ProductionHostEnvironment()));

        Assert.Contains("Cors:AllowedOrigins", exception.Message);
    }

    [Fact]
    public void Validate_AllowsCompleteProductionConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=db;Database=cabrynt;Username=cabrynt;Password=secret",
            ["Admin:Password"] = "secret",
            ["DataProtection:ApplicationName"] = "Cabrynt",
            ["DataProtection:KeyDirectory"] = "/keys",
            ["Cors:AllowedOrigins"] = "https://cabrynt.example",
            ["TripDurationModel:Enabled"] = "true",
            ["Routing:OsrmBaseUrl"] = "http://osrm:5000",
            ["TripDurationModel:ModelArtifactUrl"] = "https://github.com/nazarmikh/Cabrynt/releases/download/trip-duration-model-v1.0.0/trip-duration-residual.onnx",
            ["TripDurationModel:MetadataUrl"] = "https://github.com/nazarmikh/Cabrynt/releases/download/trip-duration-model-v1.0.0/trip-duration-residual.metadata.json",
            ["TripDurationModel:ExpectedVersion"] = "1.0.0"
        });

        ProductionConfigurationValidator.Validate(configuration, new ProductionHostEnvironment());
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class ProductionHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Cabrynt.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
