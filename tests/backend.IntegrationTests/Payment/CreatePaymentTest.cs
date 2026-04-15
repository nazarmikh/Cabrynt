using backend.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Data;
using Project.DTOs;
using Project.Enums;
using Project.Models;
using Project.Services;

namespace backend.IntegrationTests.Payment;

public class CreatePaymentTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CreatePaymentTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePaymentAsync_ReturnsNull_WhenRideIsNotCompleted()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rideId = await SeedRideAsync(appDbContext, RideStatus.InProgress, 0);

        var response = await paymentService.CreatePaymentAsync(
            new CreatePaymentRequestDto { RideId = rideId });

        Assert.Null(response);
        var paymentExistsForRide = await appDbContext.Payments
            .Include(p => p.Ride)
            .AnyAsync(p => p.Ride.Id == rideId);

        Assert.False(paymentExistsForRide);
    }

    [Fact]
    public async Task CreatePaymentAsync_PersistsPaymentAndUpdatesLoyaltyPoints_WhenRideIsCompleted()
    {
        int rideId;
        int passengerUserId;

        await using (var arrangeScope = _factory.Services.CreateAsyncScope())
        {
            var appDbContext = arrangeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            rideId = await SeedRideAsync(appDbContext, RideStatus.Completed, 500);
            passengerUserId = await appDbContext.Rides
                .Where(r => r.Id == rideId)
                .Select(r => r.PassengerProfile.UserId)
                .SingleAsync();
        }

        CreatePaymentResponseDto? response;

        await using (var actionScope = _factory.Services.CreateAsyncScope())
        {
            var paymentService = actionScope.ServiceProvider.GetRequiredService<IPaymentService>();

            response = await paymentService.CreatePaymentAsync(
                new CreatePaymentRequestDto { RideId = rideId });
        }

        Assert.NotNull(response);
        Assert.True(response!.Id > 0);
        Assert.Equal(19.97m, response.PayAmount);

        await using var assertScope = _factory.Services.CreateAsyncScope();
        var assertDbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var payment = await assertDbContext.Payments
            .Include(p => p.Ride)
            .SingleAsync(p => p.Ride.Id == rideId);

        Assert.Equal(19.97m, payment.PayAmount);
        Assert.False(string.IsNullOrWhiteSpace(payment.TransactionReference));
        Assert.StartsWith("TXN-", payment.TransactionReference);
        Assert.Equal(TransactionStatus.Successful, payment.TransactionStatus);

        var passenger = await assertDbContext.PassengerProfiles.SingleAsync(p => p.UserId == passengerUserId);
        Assert.Equal(200, passenger.Points);
    }

    private static async Task<int> SeedRideAsync(AppDbContext appDbContext, RideStatus rideStatus, int loyaltyPoints)
    {
        var timestamp = DateTime.UtcNow;
        var user = new User
        {
            Email = $"it-payment-{Guid.NewGuid():N}@novadrive.test",
            PasswordHash = "hashed-password",
            Role = Role.Passenger,
            LastLogin = timestamp,
            AccountCreated = timestamp
        };

        await appDbContext.Users.AddAsync(user);
        await appDbContext.SaveChangesAsync();

        var passenger = new PassengerProfile
        {
            UserId = user.Id,
            User = user,
            Name = "Payment Test Passenger",
            HomeAddress = "Main Street 1",
            Points = loyaltyPoints,
            PreferredPaymentMethod = "Card"
        };

        await appDbContext.PassengerProfiles.AddAsync(passenger);

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

        await appDbContext.Rides.AddAsync(ride);
        await appDbContext.SaveChangesAsync();

        return ride.Id;
    }
}
