using System.Security.Claims;
using FluentValidation;
using Project.Services;

namespace Project.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/public/tickets", async (
            ClaimsPrincipal principal,
            CreateTicketRequestDto request,
            IValidator<CreateTicketRequestDto> validator,
            ITicketService ticketService) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        ));
            }

            var response = await ticketService.CreateTicketAsync(principal, request);
            if (response is null)
            {
                return Results.Unauthorized();
            }

            return Results.Created($"/api/public/tickets/{response.Id}", response);
        }).RequireAuthorization("Passenger");

        builder.MapGet("/api/public/tickets", async (
            ClaimsPrincipal principal,
            ITicketService ticketService) =>
        {
            var response = await ticketService.GetAllTicketsAsync(principal);
            if (response is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        }).RequireAuthorization("Passenger");

        builder.MapGet("/api/public/tickets/{id:int}", async (
            ClaimsPrincipal principal,
            int id,
            ITicketService ticketService) =>
        {
            try
            {
                var response = await ticketService.GetTicketByIdAsync(principal, id);
                if (response is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }).RequireAuthorization("Passenger");

        builder.MapGet("/api/private/tickets", async (
            ITicketService ticketService) =>
        {
            var response = await ticketService.GetAdminTicketsAsync();
            return Results.Ok(response);
        }).RequireAuthorization("Admin");

        builder.MapPatch("/api/private/tickets/{id:int}/status", async (
            int id,
            UpdateTicketStatusRequestDto request,
            IValidator<UpdateTicketStatusRequestDto> validator,
            ITicketService ticketService) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        ));
            }

            var response = await ticketService.UpdateTicketStatusAsync(id, request);
            return response is null ? Results.NotFound() : Results.Ok(response);
        }).RequireAuthorization("Admin");

        return builder;
    }
}
