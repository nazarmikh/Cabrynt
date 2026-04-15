using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Data;

namespace backend.IntegrationTests.Ticket;

public class CreateTicketTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CreateTicketTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTicket_ReturnsCreated_AndPersistsTicket_WhenRequestIsValid()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var request = new
        {
            subject = "Vehicle arrived damaged",
            description = "The assigned vehicle had visible damage on the rear door.",
            ticketPriority = "High"
        };

        var response = await _client.PostAsJsonAsync("/api/public/tickets", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.True(body!["id"].GetInt32() > 0);
        Assert.Equal(request.subject, body["subject"].GetString());
        Assert.Equal(request.description, body["description"].GetString());
        Assert.Equal(request.ticketPriority, body["ticketPriority"].GetString());
        Assert.Equal("Open", body["ticketStatus"].GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var createdTicket = await dbContext.Tickets
            .Include(t => t.PassengerProfile)
            .ThenInclude(p => p.User)
            .SingleAsync(t => t.Id == body["id"].GetInt32());

        Assert.Equal(request.subject, createdTicket.Subject);
        Assert.Equal(request.description, createdTicket.Description);
        Assert.Equal(passenger.Email, createdTicket.PassengerProfile.User.Email);
    }

    [Fact]
    public async Task CreateTicket_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.PostAsJsonAsync("/api/public/tickets", new
        {
            subject = "Need help",
            description = "The app crashed during booking.",
            ticketPriority = "Medium"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_ReturnsForbidden_WhenCallerIsNotPassenger()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PostAsJsonAsync("/api/public/tickets", new
        {
            subject = "Need help",
            description = "The app crashed during booking.",
            ticketPriority = "Medium"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_ReturnsBadRequest_WhenRequestIsInvalid()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.PostAsJsonAsync("/api/public/tickets", new
        {
            subject = "",
            description = "",
            ticketPriority = "InvalidPriority"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
