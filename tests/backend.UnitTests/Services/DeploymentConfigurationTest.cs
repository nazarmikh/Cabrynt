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
            ["DataProtection:BlobUri"] = "https://cabrynt.blob.core.windows.net/data-protection/key-ring.xml",
            ["DataProtection:KeyVaultKeyIdentifier"] = "https://cabrynt.vault.azure.net/keys/data-protection",
            ["ReverseProxy:UseForwardedHeaders"] = "true",
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
            ["DataProtection:BlobUri"] = "https://cabrynt.blob.core.windows.net/data-protection/key-ring.xml",
            ["DataProtection:KeyVaultKeyIdentifier"] = "https://cabrynt.vault.azure.net/keys/data-protection",
            ["ReverseProxy:UseForwardedHeaders"] = "true",
            ["Cors:AllowedOrigins"] = "https://cabrynt.example",
            ["TripDurationModel:Enabled"] = "true",
            ["Routing:OsrmBaseUrl"] = "http://osrm:5000",
            ["TripDurationModel:ModelArtifactUrl"] = "https://github.com/nazarmikh/Cabrynt/releases/download/trip-duration-model-v1.0.0/trip-duration-residual.onnx",
            ["TripDurationModel:MetadataUrl"] = "https://github.com/nazarmikh/Cabrynt/releases/download/trip-duration-model-v1.0.0/trip-duration-residual.metadata.json",
            ["TripDurationModel:ExpectedVersion"] = "1.0.0"
        });

        ProductionConfigurationValidator.Validate(configuration, new ProductionHostEnvironment());
    }

    [Fact]
    public void Validate_RejectsMissingAzureDataProtectionConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=db;Database=cabrynt;Username=cabrynt;Password=secret",
            ["Admin:Password"] = "secret",
            ["DataProtection:ApplicationName"] = "Cabrynt",
            ["Cors:AllowedOrigins"] = "https://cabrynt.example",
            ["ReverseProxy:UseForwardedHeaders"] = "true"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(configuration, new ProductionHostEnvironment()));

        Assert.Contains("DataProtection:BlobUri", exception.Message);
        Assert.Contains("DataProtection:KeyVaultKeyIdentifier", exception.Message);
    }

    [Fact]
    public void ShouldApplyMigrations_OnlyRunsAutomaticallyInDevelopment()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var productionEnvironment = new ProductionHostEnvironment();
        var developmentEnvironment = new ProductionHostEnvironment { EnvironmentName = Environments.Development };

        Assert.False(HostingConfiguration.ShouldApplyMigrations(configuration, productionEnvironment));
        Assert.True(HostingConfiguration.ShouldApplyMigrations(configuration, developmentEnvironment));
    }

    [Fact]
    public void ShouldApplyMigrations_AllowsAnExplicitProductionMigrationJob()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:ApplyMigrationsOnStartup"] = "true"
        });

        Assert.True(HostingConfiguration.ShouldApplyMigrations(configuration, new ProductionHostEnvironment()));
    }

    [Fact]
    public void ShouldExitAfterMigrations_RequiresMigrationMode()
    {
        var exitOnlyConfiguration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:ExitAfterMigrations"] = "true"
        });
        var migrationJobConfiguration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:ApplyMigrationsOnStartup"] = "true",
            ["Database:ExitAfterMigrations"] = "true"
        });
        var productionEnvironment = new ProductionHostEnvironment();

        Assert.False(HostingConfiguration.ShouldExitAfterMigrations(exitOnlyConfiguration, productionEnvironment));
        Assert.True(HostingConfiguration.ShouldExitAfterMigrations(migrationJobConfiguration, productionEnvironment));
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
