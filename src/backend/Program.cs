using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Project.GraphQL;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Project.Services;
using ZstdSharp.Unsafe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using Project.Endpoints;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Hasher

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Repositories

builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<IRideRepository, RideRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddScoped<ISensorDiagnosticRepository, SensorDiagnosticRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();

// Services

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRideService, RideService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<ISensorDiagnosticService, SensorDiagnosticService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();


// Enums as a string not index

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Token provider for passengers

builder.Services.AddSingleton<ITokenProvider, TokenProvider>();


// Postgres

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Mongo db

builder.Services.AddSingleton<TelemetryMongoContext>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var mongoConnection = configuration.GetConnectionString("Mongo")
        ?? throw new InvalidOperationException("Missing connection string 'Mongo'.");
    var mongoDatabaseName = configuration["Mongo:DatabaseName"]
        ?? throw new InvalidOperationException("Missing Mongo database name at 'Mongo:DatabaseName'.");

    return new TelemetryMongoContext(mongoConnection, mongoDatabaseName);
});

// Validation

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
builder.Services.AddGraphQLServer()
    .AddQueryType<Query>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NovaDrive API",
        Version = "v1",
        Description = "REST endpoints for the NovaDrive assignment demo."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter only the JWT access token. Swagger will send it as 'Bearer {token}'."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Jwt key 

var jwtKey = builder.Configuration["JwtToken"]
    ?? throw new InvalidOperationException("Missing JwtToken");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });



builder.Services.AddAuthorization(o => o.AddPolicy("Admin", p => p.RequireRole("Admin"))); 
builder.Services.AddAuthorization(o => o.AddPolicy("Vehicle", p => p.RequireRole("Vehicle"))); 
builder.Services.AddAuthorization(o => o.AddPolicy("Passenger", p => p.RequireRole("Passenger"))); 
builder.Services.AddAuthorization(o => o.AddPolicy("AdminOrVehicle", p => p.RequireAssertion(ctx =>
    ctx.User.IsInRole("Admin") || ctx.User.IsInRole("Vehicle"))));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
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
            if (string.IsNullOrEmpty(builder.Configuration["Admin:Email"]) || string.IsNullOrEmpty(builder.Configuration["Admin:Password"]))
            {
                throw new ArgumentException("Admin email and password are required in .env");
            }
            User newAdmin = new User()
            {
                Role = Role.Admin,
                Email = builder.Configuration["Admin:Email"],
                PasswordHash = string.Empty,
                LastLogin = DateTime.UtcNow,
                AccountCreated = DateTime.UtcNow
            };

            newAdmin.PasswordHash = authService.HashPassword(builder.Configuration["Admin:Password"], newAdmin);

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


app.UseCors("FrontendDev");
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var mongo = scope.ServiceProvider.GetRequiredService<TelemetryMongoContext>();
    await mongo.EnsureIndexesAsync();
}

app.MapAuthEndpoints();

app.MapRideEndpoints();

app.MapVehicleEndpoints();

app.MapTelemetryEndpoints();

app.MapSensorDiagnosticEndpoints();

app.MapPaymentEndpoints();

app.MapTicketEndpoints();

app.MapMaintenanceEndpoints();
app.MapGraphQL("/graphql").RequireAuthorization("Admin");



app.Run();

public partial class Program;
