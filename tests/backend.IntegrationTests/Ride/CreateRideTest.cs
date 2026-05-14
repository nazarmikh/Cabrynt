using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Project.Data;

namespace backend.IntegrationTests.CreateRide;

public class CreateRideTest : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminEmail = "admin@cabrynt.test";
    private const string AdminPassword = "AdminPassword123!";
    private const string DefaultPassword = "StrongPass123!";

    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CreateRideTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRide_ReturnsCreatedAndPersistsCoordinates_WhenRequestIsValid()
    {
        var context = await CreateRideScenarioAsync();

        Assert.Equal(HttpStatusCode.Created, context.Response.StatusCode);
        Assert.True(context.RideBody["vehicleId"].ValueKind != JsonValueKind.Null);
        Assert.Equal("InProgress", context.RideBody["rideStatus"].GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var createdRide = await dbContext.Rides.FindAsync(context.RideId);

        Assert.NotNull(createdRide);
        Assert.Equal(context.RideRequest.DepartureLatitude, createdRide!.DepartureLatitude);
        Assert.Equal(context.RideRequest.DepartureLongitude, createdRide.DepartureLongitude);
        Assert.Equal(context.RideRequest.DestinationLatitude, createdRide.DestinationLatitude);
        Assert.Equal(context.RideRequest.DestinationLongitude, createdRide.DestinationLongitude);
    }

    [Fact]
    public async Task GetRides_ReturnsCreatedRideWithoutAssumingResponseOrder()
    {
        var context = await CreateRideScenarioAsync();

        var response = await _client.GetAsync("/api/public/rides");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rides = await response.Content.ReadFromJsonAsync<List<Dictionary<string, JsonElement>>>();

        Assert.NotNull(rides);
        Assert.NotEmpty(rides);

        var matchingRide = rides!.SingleOrDefault(ride => ride["rideId"].GetInt32() == context.RideId);

        Assert.NotNull(matchingRide);
        Assert.Equal("InProgress", matchingRide!["rideStatus"].GetString());
        Assert.True(matchingRide["vehicleId"].ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public async Task GetRide_ReturnsCreatedRideDetails_WhenRequestIsValid()
    {
        var context = await CreateRideScenarioAsync();

        var response = await _client.GetAsync($"/api/public/rides/{context.RideId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ride = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(ride);
        Assert.Equal(context.RideRequest.DepartureLocation, ride!["departureLocation"].GetString());
        Assert.Equal(context.RideRequest.DestinationLocation, ride["destinationLocation"].GetString());
        Assert.Equal("InProgress", ride["rideStatus"].GetString());
    }

    [Fact]
    public async Task CreateRide_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/public/rides", new
        {
            DepartureLocation = "Kortrijk",
            DestinationLocation = "Ghent",
            DepartureLatitude = 50.826,
            DepartureLongitude = 3.264,
            DestinationLatitude = 51.054,
            DestinationLongitude = 3.717
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRide_ReturnsBadRequest_WhenCoordinatesAreInvalid()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync("/api/public/rides", new
        {
            DepartureLocation = "Kortrijk",
            DestinationLocation = "Ghent",
            DepartureLatitude = 95.0,
            DepartureLongitude = 3.264,
            DestinationLatitude = 51.054,
            DestinationLongitude = 3.717
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRide_ReturnsForbidden_WhenRideBelongsToAnotherPassenger()
    {
        var context = await CreateRideScenarioAsync();
        var anotherPassenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var anotherPassengerToken = await IntegrationTestData.LoginAsync(_client, anotherPassenger.Email, anotherPassenger.Password);

        IntegrationTestData.Authorize(_client, anotherPassengerToken);

        var response = await _client.GetAsync($"/api/public/rides/{context.RideId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRide_DoesNotAssignVehicle_WhenNearestVehicleIsAlreadyBusy()
    {
        var random = new Random();
        var systemEmail = $"it-busy-vehicle-{Guid.NewGuid():N}@cabrynt.test";
        var firstPassengerEmail = $"it-busy-passenger-one-{Guid.NewGuid():N}@cabrynt.test";
        var secondPassengerEmail = $"it-busy-passenger-two-{Guid.NewGuid():N}@cabrynt.test";

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var vehicleId = await RegisterVehicleAsync(systemEmail);

        var vehicleToken = await LoginAsync(systemEmail, DefaultPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vehicleToken);
        await AddTelemetryAsync(vehicleId, random);

        await RegisterPassengerAsync(firstPassengerEmail);
        await RegisterPassengerAsync(secondPassengerEmail);

        var firstPassengerToken = await LoginAsync(firstPassengerEmail, DefaultPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstPassengerToken);

        var firstResponse = await _client.PostAsJsonAsync("/api/public/rides", new RideRequestPayload
        {
            DepartureLocation = "Kortrijk",
            DestinationLocation = "Ghent",
            DepartureLatitude = 50.826,
            DepartureLongitude = 3.264,
            DestinationLatitude = 51.054,
            DestinationLongitude = 3.717
        });

        firstResponse.EnsureSuccessStatusCode();

        var firstBody = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(firstBody);
        Assert.Equal("InProgress", firstBody!["rideStatus"].GetString());
        Assert.True(firstBody["vehicleId"].ValueKind != JsonValueKind.Null);

        var firstAssignedVehicleId = firstBody["vehicleId"].GetInt32();

        var secondPassengerToken = await LoginAsync(secondPassengerEmail, DefaultPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondPassengerToken);

        var secondResponse = await _client.PostAsJsonAsync("/api/public/rides", new RideRequestPayload
        {
            DepartureLocation = "Brussels",
            DestinationLocation = "Leuven",
            DepartureLatitude = 50.8503,
            DepartureLongitude = 4.3517,
            DestinationLatitude = 50.8798,
            DestinationLongitude = 4.7005
        });

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var secondBody = await secondResponse.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(secondBody);

        if (secondBody!["vehicleId"].ValueKind == JsonValueKind.Null)
        {
            Assert.Equal("Requested", secondBody["rideStatus"].GetString());
            return;
        }

        Assert.Equal("InProgress", secondBody["rideStatus"].GetString());
        Assert.NotEqual(firstAssignedVehicleId, secondBody["vehicleId"].GetInt32());
    }

    private async Task<(HttpResponseMessage Response, Dictionary<string, JsonElement> RideBody, int RideId, RideRequestPayload RideRequest)> CreateRideScenarioAsync()
    {
        var random = new Random();
        var systemEmail = $"it-create-ride-{Guid.NewGuid():N}@cabrynt.test";
        var passengerEmail = $"it-auth-{Guid.NewGuid():N}@cabrynt.test";

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var vehicleId = await RegisterVehicleAsync(systemEmail);

        var vehicleToken = await LoginAsync(systemEmail, DefaultPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vehicleToken);
        await AddTelemetryAsync(vehicleId, random);

        await RegisterPassengerAsync(passengerEmail);

        var passengerToken = await LoginAsync(passengerEmail, DefaultPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", passengerToken);

        var rideRequest = new RideRequestPayload
        {
            DepartureLocation = "Kortrijk",
            DestinationLocation = "Ghent",
            DepartureLatitude = random.NextDouble() * 180 - 90,
            DepartureLongitude = random.NextDouble() * 360 - 180,
            DestinationLatitude = random.NextDouble() * 180 - 90,
            DestinationLongitude = random.NextDouble() * 360 - 180
        };

        var response = await _client.PostAsJsonAsync("/api/public/rides", rideRequest);
        var rideBody = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(rideBody);

        return (response, rideBody!, rideBody!["rideId"].GetInt32(), rideRequest);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/public/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.NotNull(body);

        return body!["accessToken"];
    }

    private async Task<int> RegisterVehicleAsync(string systemEmail)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var response = await _client.PostAsJsonAsync("/api/private/vehicles", new
        {
            VIN = $"1HGCM82633A{suffix}",
            LicencePlate = $"TEST{suffix}",
            Model = "Toyota Camry",
            VehicleType = 0,
            Year = 2020,
            SystemEmail = systemEmail,
            SystemPassword = DefaultPassword
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);

        return body!["vehicleId"].GetInt32();
    }

    private async Task AddTelemetryAsync(int vehicleId, Random random)
    {
        var response = await _client.PostAsJsonAsync("/api/private/telemetry", new
        {
            Latitude = random.NextDouble() * 180 - 90,
            Longitude = random.NextDouble() * 360 - 180,
            CurrentSpeed = random.Next(0, 120),
            RemainingBatteryPercentage = random.Next(0, 101),
            HardwareTemperature = random.Next(5, 70),
            VehicleId = vehicleId
        });

        response.EnsureSuccessStatusCode();
    }

    private async Task RegisterPassengerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/public/auth/register", new
        {
            email,
            password = DefaultPassword,
            name = "Test User",
            homeAddress = "Main Street 1",
            preferredPaymentMethod = "Card"
        });

        response.EnsureSuccessStatusCode();
    }

    private sealed class RideRequestPayload
    {
        public required string DepartureLocation { get; init; }
        public required string DestinationLocation { get; init; }
        public double DepartureLatitude { get; init; }
        public double DepartureLongitude { get; init; }
        public double DestinationLatitude { get; init; }
        public double DestinationLongitude { get; init; }
    }
}
