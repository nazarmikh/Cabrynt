using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Project.Services;
using Project.Endpoints;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;



var builder = WebApplication.CreateBuilder(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddCabryntDataProtection(builder.Configuration);
builder.Services.AddCabryntForwardedHeaders(builder.Configuration);

// Hasher

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Repositories

builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<IRideRepository, RideRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

// Services

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRideService, RideService>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<ITicketService, TicketService>();

builder.Services.Configure<RoutingOptions>(
    builder.Configuration.GetSection(RoutingOptions.SectionName));
builder.Services.AddHttpClient<IRouteEstimator, OsrmRouteEstimator>((serviceProvider, client) =>
{
    var routingOptions = serviceProvider
        .GetRequiredService<IOptions<RoutingOptions>>()
        .Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(routingOptions.RequestTimeoutSeconds, 1, 30));
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", routingOptions.UserAgent);
});

builder.Services.AddMemoryCache();
builder.Services.Configure<WeatherOptions>(
    builder.Configuration.GetSection(WeatherOptions.SectionName));
builder.Services.AddHttpClient<IQuoteWeatherProvider, OpenMeteoWeatherProvider>((serviceProvider, client) =>
{
    var weatherOptions = serviceProvider
        .GetRequiredService<IOptions<WeatherOptions>>()
        .Value;
    client.BaseAddress = new Uri(weatherOptions.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(weatherOptions.RequestTimeoutSeconds, 1, 30));
});
builder.Services.AddSingleton<IPublicHolidayProvider, PortuguesePublicHolidayProvider>();

builder.Services.Configure<TripDurationModelOptions>(
    builder.Configuration.GetSection(TripDurationModelOptions.SectionName));
builder.Services.AddHttpClient<TripDurationModelArtifactLoader>((serviceProvider, client) =>
{
    var modelOptions = serviceProvider
        .GetRequiredService<IOptions<TripDurationModelOptions>>()
        .Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(modelOptions.DownloadTimeoutSeconds, 1, 60));
});
builder.Services.AddSingleton<ITripDurationFeatureBuilder, TripDurationFeatureBuilder>();
builder.Services.AddSingleton<TripDurationPredictorProvider>();
builder.Services.AddSingleton<ITripDurationPredictor>(serviceProvider =>
    serviceProvider.GetRequiredService<TripDurationPredictorProvider>());
builder.Services.AddSingleton<TripDurationModelStatusProvider>();
builder.Services.AddSingleton<ITripDurationModelStatusProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<TripDurationModelStatusProvider>());
builder.Services.AddHostedService<TripDurationModelInitializationService>();
builder.Services.AddScoped<ITripDurationEstimator, TripDurationEstimator>();


// Enums as a string not index

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});


// Postgres

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: ["ready"]);

// Validation

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = CorsOriginConfiguration.GetConfiguredOrigins(builder.Configuration);
        if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
        {
            allowedOrigins = ["http://localhost:3000", "http://localhost:3001"];
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("model-demo", httpContext =>
    {
        var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientAddress,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cabrynt API",
        Version = "v1",
        Description = "REST endpoints for Cabrynt's Porto ride quotation platform."
    });
});

// Cookie

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Cabrynt.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.LoginPath = "/api/public/auth/login";
        options.LogoutPath = "/api/public/auth/logout";
        options.AccessDeniedPath = "/api/public/auth/forbidden";

        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });



builder.Services.AddAuthorization(o => o.AddPolicy("Admin", p => p.RequireRole("Admin")));
builder.Services.AddAuthorization(o => o.AddPolicy("Passenger", p => p.RequireRole("Passenger")));

var app = builder.Build();

if (HostingConfiguration.UsesForwardedHeaders(app.Configuration))
{
    app.UseForwardedHeaders();
}

app.UseSwagger();
app.UseSwaggerUI();

if (HostingConfiguration.ShouldApplyMigrations(app.Configuration, app.Environment))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (HostingConfiguration.ShouldExitAfterMigrations(app.Configuration, app.Environment))
{
    return;
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    try
    {
        User? adminUser = await db.Users.FirstOrDefaultAsync(x => x.Role == Role.Admin);
        if (adminUser == null)
        {
            string? adminEmail = builder.Configuration["Admin:Email"];
            string? adminPassword = builder.Configuration["Admin:Password"];
            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                throw new ArgumentException("Admin email and password are required in .env");
            }
            User newAdmin = new User()
            {
                Role = Role.Admin,
                Email = adminEmail,
                PasswordHash = string.Empty,
                LastLogin = DateTime.UtcNow,
                AccountCreated = DateTime.UtcNow
            };

            newAdmin.PasswordHash = authService.HashPassword(adminPassword, newAdmin);

            await db.Users.AddAsync(newAdmin);
            await db.SaveChangesAsync();

        }

    }
    catch (DbUpdateException)
    {
        var adminExists = await db.Users.AnyAsync(x => x.Role == Role.Admin);
        if (!adminExists)
        {
            throw new InvalidOperationException("Failed to create admin user.");
        }
    }

}


app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapAuthEndpoints();

app.MapRideEndpoints();

app.MapTripDurationModelEndpoints();

app.MapModelInsightsEndpoints();

app.MapTicketEndpoints();


app.Run();

public partial class Program;
