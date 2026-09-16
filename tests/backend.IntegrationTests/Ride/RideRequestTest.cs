using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Ride;

public class RideRequestTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RideRequestTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClientWithoutCookies();
    }

    [Fact]
    public async Task CreateRide_ReturnsRequestedRide_WhenPassengerIsAuthenticated()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);

        var response = await _client.PostAsJsonAsync("/api/public/rides", CreateRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.Equal("Requested", body!["rideStatus"].GetString());
        Assert.True(body["estimatedPrice"].GetDecimal() > 0m);
        Assert.Equal("StraightLineFallback", body["estimatedTripDurationSource"].GetString());
        Assert.Equal(body["duration"].GetDecimal(), body["estimatedTripDuration"].GetDecimal());
    }

    [Fact]
    public async Task CancelRide_ReturnsNoContent_WhenPassengerOwnsRequestedRide()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        var created = await _client.PostAsJsonAsync("/api/public/rides", CreateRequest());
        var body = await created.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        var response = await _client.DeleteAsync($"/api/public/rides/{body!["rideId"].GetInt32()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static object CreateRequest()
    {
        return new
        {
            departureLocation = "Aliados",
            destinationLocation = "Boavista",
            departureLatitude = 41.149,
            departureLongitude = -8.611,
            destinationLatitude = 41.16,
            destinationLongitude = -8.64,
            preferredVehicleType = "Standard"
        };
    }
}
