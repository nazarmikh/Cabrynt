using System.Security.Claims;
using FluentValidation;
using Project.Services;

namespace Project.Endpoints;

public static class RideEndpoints
{
    public static IEndpointRouteBuilder MapRideEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/public/rides", async (
            RideRequestDto request,
            IValidator<RideRequestDto> validator,
            IRideService rideService,
            ClaimsPrincipal token) =>
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

            try
            {
                RideResponseDto? ride = await rideService.CreateRideAsync(token, request);
                if (ride is null)
                    return Results.Unauthorized();
                return Results.Created($"/api/public/rides/{ride.RideId}", ride);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem("Failed to create ride.");
            }
        }).RequireAuthorization("Passenger");


        app.MapGet("/api/public/rides", async (
            IRideService rideService,
            ClaimsPrincipal token) =>
        {


            try
            {
                List<RideResponseDto>? response = await rideService.GetAllRidesAsync(token);
                if (response is null)
                {
                    return Results.Unauthorized();
                }
                return Results.Ok(response);
            }
            catch (Exception)
            {
                return Results.Problem("Failed to get rides.");
            }
        }).RequireAuthorization("Passenger");


        app.MapGet("/api/public/rides/{rideId}", async (
            int rideId,
            IRideService rideService,
            ClaimsPrincipal token) =>
        {


            try
            {
                GetRideByIdResponseDto? response = await rideService.GetRideByIdAsync(token, rideId);
                if (response is null)
                {
                    return Results.Unauthorized();
                }
                return Results.Ok(response);
            }
            catch(UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception)
            {
                return Results.Problem("Failed to get ride.");
            }
        }).RequireAuthorization("Passenger");
        

        return app;
    }

}
