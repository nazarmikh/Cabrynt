using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Services;

namespace backend.IntegrationTests.CreateRide;

public class RideQuoteMlInferenceTest
{
    [Fact]
    public async Task GetRideQuote_ReturnsMachineLearningEstimate_WhenAllRequiredContextIsAvailable()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sum-23-features.onnx");
        using var factory = new CustomWebApplicationFactory(
            new Dictionary<string, string?>
            {
                ["TripDurationModel:Enabled"] = "true",
                ["TripDurationModel:ModelPath"] = modelPath
            },
            services =>
            {
                services.RemoveAll<IRouteEstimator>();
                services.AddScoped<IRouteEstimator, StubRouteEstimator>();
                services.RemoveAll<IQuoteWeatherProvider>();
                services.AddScoped<IQuoteWeatherProvider, StubWeatherProvider>();
            });
        var client = factory.CreateClientWithoutCookies();
        var passenger = await IntegrationTestData.RegisterPassengerAsync(client);
        var passengerCookie = await IntegrationTestData.LoginAsync(client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(client, passengerCookie);

        var response = await client.PostAsJsonAsync("/api/public/rides/quote", new
        {
            departureLocation = "Porto centre",
            destinationLocation = "Porto airport",
            departureLatitude = 41.1579,
            departureLongitude = -8.6291,
            destinationLatitude = 41.2356,
            destinationLongitude = -8.6783,
            preferredVehicleType = "Standard"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.Equal("MachineLearning", body!["estimatedTripDurationSource"].GetString());
        Assert.True(body["estimatedTripDuration"].GetDecimal() > body["duration"].GetDecimal());
    }

    private sealed class StubRouteEstimator : IRouteEstimator
    {
        public Task<RouteEstimate> EstimateAsync(
            RouteRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RouteEstimate(12.5, 20, RouteEstimateSource.Osrm));
        }
    }

    private sealed class StubWeatherProvider : IQuoteWeatherProvider
    {
        public Task<WeatherSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WeatherSnapshot?>(new WeatherSnapshot(18, 0, 50, 12));
        }
    }
}
