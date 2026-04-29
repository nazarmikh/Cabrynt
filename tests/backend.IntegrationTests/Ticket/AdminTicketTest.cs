using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Data;
using Project.Enums;

namespace backend.IntegrationTests.Ticket;

public class AdminTicketTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AdminTicketTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAdminTickets_ReturnsAllTickets_WhenCallerIsAdmin()
    {
        var firstPassenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var firstPassengerToken = await IntegrationTestData.LoginAsync(_client, firstPassenger.Email, firstPassenger.Password);
        IntegrationTestData.Authorize(_client, firstPassengerToken);
        await CreateTicketAsync("Billing issue", "Fare mismatch", "High");

        var secondPassenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var secondPassengerToken = await IntegrationTestData.LoginAsync(_client, secondPassenger.Email, secondPassenger.Password);
        IntegrationTestData.Authorize(_client, secondPassengerToken);
        await CreateTicketAsync("App crash", "Booking screen froze", "Critical");

        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.GetAsync("/api/private/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tickets = await response.Content.ReadFromJsonAsync<List<Dictionary<string, JsonElement>>>();

        Assert.NotNull(tickets);
        Assert.True(tickets!.Count >= 2);
        Assert.Contains(tickets, x => x["passengerEmail"].GetString() == firstPassenger.Email);
        Assert.Contains(tickets, x => x["passengerEmail"].GetString() == secondPassenger.Email);
    }

    [Fact]
    public async Task UpdateTicketStatus_ReturnsOk_AndPersistsNewStatus_WhenCallerIsAdmin()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);
        var ticketId = await CreateTicketAsync("Need support", "Driverless taxi stopped unexpectedly", "Medium");

        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PatchAsJsonAsync($"/api/private/tickets/{ticketId}/status", new
        {
            ticketStatus = "Resolved"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);
        Assert.Equal("Resolved", body!["ticketStatus"].GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedTicket = await dbContext.Tickets.SingleAsync(x => x.Id == ticketId);
        Assert.Equal(TicketStatus.Resolved, updatedTicket.TicketStatus);
    }

    [Fact]
    public async Task UpdateTicketStatus_ReturnsNotFound_WhenTicketDoesNotExist()
    {
        var adminToken = await IntegrationTestData.LoginAsAdminAsync(_client);
        IntegrationTestData.Authorize(_client, adminToken);

        var response = await _client.PatchAsJsonAsync("/api/private/tickets/999999/status", new
        {
            ticketStatus = "Resolved"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTicketStatus_ReturnsForbidden_WhenCallerIsPassenger()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);
        var ticketId = await CreateTicketAsync("Need support", "Driverless taxi stopped unexpectedly", "Medium");

        var response = await _client.PatchAsJsonAsync($"/api/private/tickets/{ticketId}/status", new
        {
            ticketStatus = "Resolved"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminTickets_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.GetAsync("/api/private/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<int> CreateTicketAsync(string subject, string description, string ticketPriority)
    {
        var response = await _client.PostAsJsonAsync("/api/public/tickets", new
        {
            subject,
            description,
            ticketPriority
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        Assert.NotNull(body);

        return body!["id"].GetInt32();
    }
}
