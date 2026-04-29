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
    private readonly string _postgresDatabaseName = $"NovaDrivePostgresTests_{Guid.NewGuid():N}";
    private readonly string _mongoDatabaseName = $"NovaDriveMongoTests_{Guid.NewGuid():N}";
    private readonly string _postgresConnectionString;
    private readonly string _mongoConnectionString;

    public CustomWebApplicationFactory()
    {
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
                ["Mongo__DatabaseName"] = _mongoDatabaseName
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupTestDatabases();
        }

        base.Dispose(disposing);
    }

    private static string BuildPostgresConnectionString(string databaseName)
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=NovaDrivePostgres;Username=postgres;Password=postgres";

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
