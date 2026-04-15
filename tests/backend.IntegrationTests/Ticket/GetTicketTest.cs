using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.IntegrationTests.Infrastructure;

namespace backend.IntegrationTests.Ticket;

public class GetTicketTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetTicketTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTickets_ReturnsOnlyCurrentPassengerTickets()
    {
        var firstPassenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var firstPassengerToken = await IntegrationTestData.LoginAsync(_client, firstPassenger.Email, firstPassenger.Password);
        IntegrationTestData.Authorize(_client, firstPassengerToken);

        await CreateTicketAsync("Billing issue", "The estimated fare looked incorrect.", "High");
        await CreateTicketAsync("Damaged vehicle", "Rear door had a visible dent.", "Medium");

        var secondPassenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var secondPassengerToken = await IntegrationTestData.LoginAsync(_client, secondPassenger.Email, secondPassenger.Password);
        IntegrationTestData.Authorize(_client, secondPassengerToken);

        await CreateTicketAsync("App crash", "The app closed when I confirmed the ride.", "Low");

        IntegrationTestData.Authorize(_client, firstPassengerToken);

        var response = await _client.GetAsync("/api/public/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);

        var tickets = body!["tickets"].EnumerateArray().ToList();
        var subjects = tickets.Select(t => t.GetProperty("subject").GetString()).ToList();

        Assert.Equal(2, tickets.Count);
        Assert.Contains("Billing issue", subjects);
        Assert.Contains("Damaged vehicle", subjects);
        Assert.DoesNotContain("App crash", subjects);
    }

    [Fact]
    public async Task GetTickets_ReturnsEmptyList_WhenPassengerHasNoTickets()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.GetAsync("/api/public/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.Empty(body!["tickets"].EnumerateArray());
    }

    [Fact]
    public async Task GetTickets_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.GetAsync("/api/public/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTicketById_ReturnsTicket_WhenItBelongsToCurrentPassenger()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var ticketId = await CreateTicketAsync("Need support", "Pickup point was wrong in the app.", "Critical");

        var response = await _client.GetAsync($"/api/public/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(body);
        Assert.Equal(ticketId, body!["id"].GetInt32());
        Assert.Equal("Need support", body["subject"].GetString());
        Assert.Equal("Critical", body["ticketPriority"].GetString());
        Assert.Equal("Open", body["ticketStatus"].GetString());
    }

    [Fact]
    public async Task GetTicketById_ReturnsForbidden_WhenTicketBelongsToAnotherPassenger()
    {
        var firstPassenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var firstPassengerToken = await IntegrationTestData.LoginAsync(_client, firstPassenger.Email, firstPassenger.Password);
        IntegrationTestData.Authorize(_client, firstPassengerToken);

        var ticketId = await CreateTicketAsync("Private ticket", "This ticket belongs to the first passenger.", "High");

        var secondPassenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var secondPassengerToken = await IntegrationTestData.LoginAsync(_client, secondPassenger.Email, secondPassenger.Password);
        IntegrationTestData.Authorize(_client, secondPassengerToken);

        var response = await _client.GetAsync($"/api/public/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTicketById_ReturnsNotFound_WhenTicketDoesNotExist()
    {
        var passenger = await IntegrationTestData.RegisterPassengerAsync(_client);
        var passengerToken = await IntegrationTestData.LoginAsync(_client, passenger.Email, passenger.Password);
        IntegrationTestData.Authorize(_client, passengerToken);

        var response = await _client.GetAsync("/api/public/tickets/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTicketById_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.GetAsync("/api/public/tickets/1");

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
