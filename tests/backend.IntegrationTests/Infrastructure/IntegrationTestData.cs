using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Project.DTOs;

namespace backend.IntegrationTests.Infrastructure;

internal static class IntegrationTestData
{
    internal const string AdminEmail = "admin@cabrynt.test";
    internal const string AdminPassword = "AdminPassword123!";
    internal const string DefaultPassword = "StrongPass123!";

    private const string AuthCookieName = "Cabrynt.Auth";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    internal static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/public/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        Assert.NotNull(body);
        Assert.True(body.Id > 0);
        Assert.NotEmpty(body.Email);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var authCookie = Assert.Single(cookies, c => c.StartsWith($"{AuthCookieName}=", StringComparison.Ordinal));

        var sessionCookie = authCookie.Split(';', 2)[0];
        Authorize(client, sessionCookie);

        return sessionCookie;
    }

    internal static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        return await LoginAsync(client, AdminEmail, AdminPassword);
    }

    internal static async Task<PassengerRegistration> RegisterPassengerAsync(HttpClient client, string? email = null)
    {
        var registration = CreatePassengerRegistration(email);

        var response = await client.PostAsJsonAsync("/api/public/auth/register", registration);

        response.EnsureSuccessStatusCode();

        return registration;
    }

    internal static PassengerRegistration CreatePassengerRegistration(string? email = null)
    {
        return new PassengerRegistration
        {
            Email = email ?? $"it-auth-{Guid.NewGuid():N}@cabrynt.test",
            Password = DefaultPassword,
            Name = "Test User",
            HomeAddress = "Main Street 1",
            PreferredPaymentMethod = "Card"
        };
    }

    internal static async Task<VehicleRegistrationResult> RegisterVehicleAsync(HttpClient client, string? systemEmail = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var request = new VehicleRegistrationRequest
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = systemEmail ?? $"it-register-vehicle-{Guid.NewGuid():N}@cabrynt.test",
            SystemPassword = DefaultPassword
        };

        var response = await client.PostAsJsonAsync("/api/private/vehicles", request);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);

        return new VehicleRegistrationResult
        {
            Request = request,
            VehicleId = body!["vehicleId"].GetInt32(),
            VIN = body["vin"].GetString()!,
            LicencePlate = body["licencePlate"].GetString()!,
            Model = body["model"].GetString()!,
            Year = body["year"].GetInt32(),
            VehicleType = body["vehicleType"].GetString()!,
            VehicleStatus = body["vehicleStatus"].GetString()!
        };
    }

    internal static void Authorize(HttpClient client, string authCookie)
    {
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", authCookie);
    }

    internal sealed class PassengerRegistration
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
        public required string Name { get; init; }
        public required string HomeAddress { get; init; }
        public required string PreferredPaymentMethod { get; init; }
    }

    internal sealed class VehicleRegistrationRequest
    {
        public required string VIN { get; init; }
        public required string LicencePlate { get; init; }
        public required string Model { get; init; }
        public int VehicleType { get; init; }
        public int Year { get; init; }
        public required string SystemEmail { get; init; }
        public required string SystemPassword { get; init; }
    }

    internal sealed class VehicleRegistrationResult
    {
        public required VehicleRegistrationRequest Request { get; init; }
        public int VehicleId { get; init; }
        public required string VIN { get; init; }
        public required string LicencePlate { get; init; }
        public required string Model { get; init; }
        public int Year { get; init; }
        public required string VehicleType { get; init; }
        public required string VehicleStatus { get; init; }
    }
}
