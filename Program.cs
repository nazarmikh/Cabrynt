using Project.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<TelemetryMongoContext>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var mongoConnection = configuration.GetConnectionString("Mongo")
        ?? throw new InvalidOperationException("Missing connection string 'Mongo'.");
    var mongoDatabaseName = configuration["Mongo:DatabaseName"]
        ?? throw new InvalidOperationException("Missing Mongo database name at 'Mongo:DatabaseName'.");

    return new TelemetryMongoContext(mongoConnection, mongoDatabaseName);
});



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var mongo = scope.ServiceProvider.GetRequiredService<TelemetryMongoContext>();
    await mongo.EnsureIndexesAsync();
}

app.MapGet("/", () => "Hello World!");

app.Run();
