using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Data;
using Project.Enums;
using Project.Models;

namespace backend.IntegrationTests.Payment;

public class PaymentEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentEndpointTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePaymentEndpoint_ReturnsCreated_AndPersistsPayment_WhenRideIsCompleted()
    {
        var rideId = await SeedRideAsync(RideStatus.Completed, 500);
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsJsonAsync($"/api/private/rides/{rideId}/payments", new
        {
            rideId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.True(body!["id"].GetInt32() > 0);
        Assert.Equal(19.97m, body["payAmount"].GetDecimal());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var payment = await dbContext.Payments
            .Include(p => p.Ride)
            .SingleAsync(p => p.Ride.Id == rideId);

        Assert.Equal(TransactionStatus.Successful, payment.TransactionStatus);
        Assert.False(string.IsNullOrWhiteSpace(payment.TransactionReference));
        Assert.StartsWith("TXN-", payment.TransactionReference);

        var passengerPoints = await dbContext.Rides
            .Where(r => r.Id == rideId)
            .Select(r => r.PassengerProfile.Points)
            .SingleAsync();

        Assert.Equal(200, passengerPoints);
    }

    [Fact]
    public async Task CreatePaymentEndpoint_ReturnsBadRequest_WhenRideIsNotCompleted()
    {
        var rideId = await SeedRideAsync(RideStatus.InProgress, 0);
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsJsonAsync($"/api/private/rides/{rideId}/payments", new
        {
            rideId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentEndpoint_ReturnsNotFound_WhenRideDoesNotExist()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsJsonAsync("/api/private/rides/999999/payments", new
        {
            rideId = 999999
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentEndpoint_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var rideId = await SeedRideAsync(RideStatus.Completed, 0);

        var response = await _client.PostAsJsonAsync($"/api/private/rides/{rideId}/payments", new
        {
            rideId
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentEndpoint_ReturnsForbidden_WhenPassengerCallsPrivateRoute()
    {
        var rideId = await SeedRideAsync(RideStatus.Completed, 0);
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync($"/api/private/rides/{rideId}/payments", new
        {
            rideId
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentEndpoint_ReturnsBadRequest_WhenRideIdIsInvalid()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsJsonAsync("/api/private/rides/0/payments", new
        {
            rideId = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentEndpoint_ReturnsBadRequest_WhenRouteAndBodyRideIdDoNotMatch()
    {
        var rideId = await SeedRideAsync(RideStatus.Completed, 0);
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsJsonAsync($"/api/private/rides/{rideId}/payments", new
        {
            rideId = rideId + 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> SeedRideAsync(RideStatus rideStatus, int loyaltyPoints)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var timestamp = new DateTime(2026, 4, 16, 12, 0, 0, DateTimeKind.Utc);
        var user = new User
        {
            Email = $"it-payment-endpoint-{Guid.NewGuid():N}@novadrive.test",
            PasswordHash = "hashed-password",
            Role = Role.Passenger,
            LastLogin = timestamp,
            AccountCreated = timestamp
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var passenger = new PassengerProfile
        {
            UserId = user.Id,
            User = user,
            Name = "Payment Endpoint Passenger",
            HomeAddress = "Main Street 1",
            Points = loyaltyPoints,
            PreferredPaymentMethod = PaymentMethod.Card
        };

        await dbContext.PassengerProfiles.AddAsync(passenger);

        var ride = new Ride
        {
            DepartureLocation = "Kortrijk",
            DepartureLatitude = 50.826,
            DepartureLongitude = 3.264,
            DestinationLocation = "Ghent",
            DestinationLatitude = 51.054,
            DestinationLongitude = 3.717,
            Distance = 10m,
            Duration = 20m,
            PreferredVehicleType = VehicleType.Standard,
            EstimatedPrice = 23.60m,
            DiscountCode = null,
            RideStatus = rideStatus,
            RequestTime = timestamp,
            PassengerProfile = passenger,
            Vehicle = null
        };

        await dbContext.Rides.AddAsync(ride);
        await dbContext.SaveChangesAsync();

        return ride.Id;
    }
}
