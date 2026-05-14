using System.Net;
using System.Net.Http.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Data;
using Project.Enums;
using Project.Models;

namespace backend.IntegrationTests.CreateRide;

public class CompleteRideTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CompleteRideTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteRide_ReturnsOk_WhenAdminCompletesInProgressRide()
    {
        var scenario = await SeedRideScenarioAsync(RideStatus.InProgress);
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsync($"/api/private/rides/{scenario.RideId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ride = await dbContext.Rides.FindAsync(scenario.RideId);
        var payment = await dbContext.Payments
            .Include(x => x.Ride)
            .SingleOrDefaultAsync(x => x.Ride.Id == scenario.RideId);

        Assert.NotNull(ride);
        Assert.Equal(RideStatus.Completed, ride!.RideStatus);
        Assert.NotNull(payment);
    }

    [Fact]
    public async Task CompleteRide_ReturnsOk_WhenAssignedVehicleCompletesOwnRide()
    {
        var scenario = await SeedRideScenarioAsync(RideStatus.InProgress);
        IntegrationTestData.Authorize(_client, scenario.AssignedVehicleToken);

        var response = await _client.PostAsync($"/api/private/rides/{scenario.RideId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteRide_ReturnsForbidden_WhenAnotherVehicleCompletesRide()
    {
        var scenario = await SeedRideScenarioAsync(RideStatus.InProgress);

        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);
        var anotherVehicle = await IntegrationTestData.RegisterVehicleAsync(_client);
        var anotherVehicleToken = await IntegrationTestData.LoginAsync(_client, anotherVehicle.Request.SystemEmail, anotherVehicle.Request.SystemPassword);

        IntegrationTestData.Authorize(_client, anotherVehicleToken);

        var response = await _client.PostAsync($"/api/private/rides/{scenario.RideId}/complete", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompleteRide_ReturnsBadRequest_WhenRideIsNotInProgress()
    {
        var scenario = await SeedRideScenarioAsync(RideStatus.Requested);
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsync($"/api/private/rides/{scenario.RideId}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteRide_ReturnsNotFound_WhenRideDoesNotExist()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsync("/api/private/rides/999999/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteRide_ReturnsForbidden_WhenPassengerCallsPrivateEndpoint()
    {
        var scenario = await SeedRideScenarioAsync(RideStatus.InProgress);
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsync($"/api/private/rides/{scenario.RideId}/complete", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompleteRide_AssignsFreedVehicleToOldestWaitingRideOfSameType()
    {
        var scenario = await SeedWaitingRideScenarioAsync();
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsync($"/api/private/rides/{scenario.InProgressRideId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var completedRide = await dbContext.Rides
            .Include(x => x.Vehicle)
            .SingleAsync(x => x.Id == scenario.InProgressRideId);

        var waitingRide = await dbContext.Rides
            .Include(x => x.Vehicle)
            .SingleAsync(x => x.Id == scenario.WaitingRideId);

        var vehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == scenario.VehicleId);

        Assert.Equal(RideStatus.Completed, completedRide.RideStatus);
        Assert.Equal(RideStatus.InProgress, waitingRide.RideStatus);
        Assert.NotNull(waitingRide.Vehicle);
        Assert.Equal(scenario.VehicleId, waitingRide.Vehicle!.Id);
        Assert.Equal(VehicleStatus.InRide, vehicle.VehicleStatus);
    }

    private async Task<(int RideId, string AssignedVehicleToken)> SeedRideScenarioAsync(RideStatus rideStatus)
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var assignedVehicle = await IntegrationTestData.RegisterVehicleAsync(_client);
        var assignedVehicleToken = await IntegrationTestData.LoginAsync(_client, assignedVehicle.Request.SystemEmail, assignedVehicle.Request.SystemPassword);

        var passengerRegistration = await IntegrationTestData.RegisterPassengerAsync(_client);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var passenger = await dbContext.PassengerProfiles
            .Include(x => x.User)
            .SingleAsync(x => x.User.Email == passengerRegistration.Email);

        var vehicle = await dbContext.Vehicles
            .SingleAsync(x => x.Id == assignedVehicle.VehicleId);

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
            PreferredVehicleType = vehicle.VehicleType,
            EstimatedPrice = 19.97m,
            DiscountCode = null,
            RideStatus = rideStatus,
            RequestTime = new DateTime(2026, 4, 16, 12, 0, 0, DateTimeKind.Utc),
            PassengerProfile = passenger,
            Vehicle = vehicle
        };

        vehicle.VehicleStatus = rideStatus == RideStatus.InProgress
            ? VehicleStatus.InRide
            : VehicleStatus.Active;

        await dbContext.Rides.AddAsync(ride);
        await dbContext.SaveChangesAsync();

        return (ride.Id, assignedVehicleToken);
    }

    private async Task<(int InProgressRideId, int WaitingRideId, int VehicleId)> SeedWaitingRideScenarioAsync()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var assignedVehicle = await IntegrationTestData.RegisterVehicleAsync(_client, $"it-queue-vehicle-{Guid.NewGuid():N}@cabrynt.test");
        var firstPassengerRegistration = await IntegrationTestData.RegisterPassengerAsync(_client, $"it-queue-passenger-one-{Guid.NewGuid():N}@cabrynt.test");
        var secondPassengerRegistration = await IntegrationTestData.RegisterPassengerAsync(_client, $"it-queue-passenger-two-{Guid.NewGuid():N}@cabrynt.test");

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var firstPassenger = await dbContext.PassengerProfiles
            .Include(x => x.User)
            .SingleAsync(x => x.User.Email == firstPassengerRegistration.Email);

        var secondPassenger = await dbContext.PassengerProfiles
            .Include(x => x.User)
            .SingleAsync(x => x.User.Email == secondPassengerRegistration.Email);

        var vehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == assignedVehicle.VehicleId);
        vehicle.VehicleStatus = VehicleStatus.InRide;

        var inProgressRide = new Ride
        {
            DepartureLocation = "Kortrijk",
            DepartureLatitude = 50.826,
            DepartureLongitude = 3.264,
            DestinationLocation = "Ghent",
            DestinationLatitude = 51.054,
            DestinationLongitude = 3.717,
            Distance = 10m,
            Duration = 20m,
            PreferredVehicleType = vehicle.VehicleType,
            EstimatedPrice = 19.97m,
            DiscountCode = null,
            RideStatus = RideStatus.InProgress,
            RequestTime = new DateTime(2026, 4, 16, 12, 0, 0, DateTimeKind.Utc),
            PassengerProfile = firstPassenger,
            Vehicle = vehicle
        };

        var waitingRide = new Ride
        {
            DepartureLocation = "Brussels",
            DepartureLatitude = 50.8503,
            DepartureLongitude = 4.3517,
            DestinationLocation = "Leuven",
            DestinationLatitude = 50.8798,
            DestinationLongitude = 4.7005,
            Distance = 5m,
            Duration = 10m,
            PreferredVehicleType = vehicle.VehicleType,
            EstimatedPrice = 12.50m,
            DiscountCode = null,
            RideStatus = RideStatus.Requested,
            RequestTime = new DateTime(2026, 4, 16, 12, 5, 0, DateTimeKind.Utc),
            PassengerProfile = secondPassenger,
            Vehicle = null
        };

        await dbContext.Rides.AddRangeAsync(inProgressRide, waitingRide);
        await dbContext.SaveChangesAsync();

        return (inProgressRide.Id, waitingRide.Id, vehicle.Id);
    }
}
