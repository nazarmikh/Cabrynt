using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.CreateRide;

public class RideQuoteTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RideQuoteTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRideQuote_ReturnsPriceBreakdown_WhenRequestIsValid()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync("/api/public/rides/quote", new
        {
            departureLocation = "Kortrijk",
            destinationLocation = "Ghent",
            departureLatitude = 50.826,
            departureLongitude = 3.264,
            destinationLatitude = 51.054,
            destinationLongitude = 3.717,
            preferredVehicleType = "Standard"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.True(body!["distance"].GetDecimal() > 0);
        Assert.True(body["duration"].GetDecimal() > 0);
        Assert.Equal(2.5m, body["baseFare"].GetDecimal());
        Assert.Equal(1m, body["vehicleMultiplier"].GetDecimal());
        Assert.True(body["estimatedPrice"].GetDecimal() > 0);
    }

    [Fact]
    public async Task GetRideQuote_ReturnsBadRequest_WhenDiscountCodeIsInvalid()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync("/api/public/rides/quote", new
        {
            departureLocation = "Kortrijk",
            destinationLocation = "Ghent",
            departureLatitude = 50.826,
            departureLongitude = 3.264,
            destinationLatitude = 51.054,
            destinationLongitude = 3.717,
            preferredVehicleType = "Standard",
            discountCode = "NOTREAL"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRideQuote_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/public/rides/quote", new
        {
            departureLocation = "Kortrijk",
            destinationLocation = "Ghent",
            departureLatitude = 50.826,
            departureLongitude = 3.264,
            destinationLatitude = 51.054,
            destinationLongitude = 3.717,
            preferredVehicleType = "Standard"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
