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

        app.MapPost("/api/public/rides/quote", async (
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
                var response = await rideService.GetRideQuoteAsync(token, request);
                return response is null ? Results.Unauthorized() : Results.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem("Failed to calculate ride quote.");
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
                var result = await rideService.GetRideByIdAsync(token, rideId);
                return result.Status switch
                {
                    RideRequestOperationStatus.Success => Results.Ok(result.Ride),
                    RideRequestOperationStatus.NotFound => Results.NotFound(),
                    RideRequestOperationStatus.Forbidden => Results.Forbid(),
                    _ => Results.Unauthorized()
                };
            }
            catch (Exception)
            {
                return Results.Problem("Failed to get ride.");
            }
        }).RequireAuthorization("Passenger");

        app.MapDelete("/api/public/rides/{rideId:int}", async (
            int rideId,
            IRideService rideService,
            ClaimsPrincipal token) =>
        {
            try
            {
                var status = await rideService.CancelRideAsync(token, rideId);
                return status switch
                {
                    RideRequestOperationStatus.Success => Results.NoContent(),
                    RideRequestOperationStatus.NotFound => Results.NotFound(),
                    RideRequestOperationStatus.Forbidden => Results.Forbid(),
                    RideRequestOperationStatus.Conflict => Results.Conflict(new
                    {
                        message = "Only requested rides can be canceled."
                    }),
                    _ => Results.Unauthorized()
                };
            }
            catch (Exception)
            {
                return Results.Problem("Failed to cancel ride.");
            }
        }).RequireAuthorization("Passenger");


        return app;
    }

}
