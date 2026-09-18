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

}
