using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Npgsql;
using Project.Data;

namespace backend.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _postgresDatabaseName = $"CabryntPostgresTests_{Guid.NewGuid():N}";
    private readonly string _mongoDatabaseName = $"CabryntMongoTests_{Guid.NewGuid():N}";
    private readonly string _postgresConnectionString;
    private readonly string _mongoConnectionString;
    private readonly Dictionary<string, string?> _previousEnvironmentValues = new();

    public CustomWebApplicationFactory()
    {
        ConfigureTestEnvironment();

        _postgresConnectionString = BuildPostgresConnectionString(_postgresDatabaseName);
        _mongoConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Mongo")
            ?? "mongodb://localhost:27017";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgresConnectionString,
                ["ConnectionStrings__Postgres"] = _postgresConnectionString,
                ["ConnectionStrings:Mongo"] = _mongoConnectionString,
                ["ConnectionStrings__Mongo"] = _mongoConnectionString,
                ["Mongo:DatabaseName"] = _mongoDatabaseName,
                ["Mongo__DatabaseName"] = _mongoDatabaseName,
                ["Admin:Email"] = IntegrationTestData.AdminEmail,
                ["Admin__Email"] = IntegrationTestData.AdminEmail,
                ["Admin:Password"] = IntegrationTestData.AdminPassword,
                ["Admin__Password"] = IntegrationTestData.AdminPassword,
                ["Email:FromAddress"] = "no-reply@cabrynt.test",
                ["Email:PickupDirectory"] = "GeneratedEmails"
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
        });
    }

    public HttpClient CreateClientWithoutCookies()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupTestDatabases();
            RestoreTestEnvironment();
        }

        base.Dispose(disposing);
    }

    private void ConfigureTestEnvironment()
    {
        SetEnvironmentVariable("JwtToken", "integration-test-secret-key-with-enough-length");
        SetEnvironmentVariable("Jwt__Issuer", "Cabrynt");
        SetEnvironmentVariable("Jwt__Audience", "CabryntClient");
        SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        SetEnvironmentVariable("Admin__Email", IntegrationTestData.AdminEmail);
        SetEnvironmentVariable("Admin__Password", IntegrationTestData.AdminPassword);
        SetEnvironmentVariable("Email__FromAddress", "no-reply@cabrynt.test");
        SetEnvironmentVariable("Email__PickupDirectory", "GeneratedEmails");
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        _previousEnvironmentValues.TryAdd(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }

    private void RestoreTestEnvironment()
    {
        foreach (var (key, value) in _previousEnvironmentValues)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string BuildPostgresConnectionString(string databaseName)
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=CabryntPostgres;Username=postgres;Password=postgres";

        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = databaseName
        };

        return builder.ConnectionString;
    }

    private void CleanupTestDatabases()
    {
        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
        }
        catch
        {
            // Ignore test database cleanup failures to avoid masking test results.
        }

        try
        {
            var mongoClient = new MongoClient(_mongoConnectionString);
            mongoClient.DropDatabase(_mongoDatabaseName);
        }
        catch
        {
            // Ignore test database cleanup failures to avoid masking test results.
        }
    }
}
