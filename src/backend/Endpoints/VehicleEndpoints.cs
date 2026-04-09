using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FluentValidation;
using Project.Services;

namespace Project.Endpoints;

public static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/private/vehicles", async (
            RegisterVehicleRequestDto request,
            IValidator<RegisterVehicleRequestDto> validator,
            IVehicleService vehicleService) =>
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
               RegisterVehicleResponseDto? response = await vehicleService.RegisterVehicleAsync(request);
               if (response == null)
                   return Results.Problem("Failed to register the vehicle.");
               return Results.Created($"/api/private/vehicles/{response.VehicleId}", response);
           }
           catch(DbUpdateException)
           {
               return Results.Conflict("A vehicle with the same VIN or licence plate already exists.");
           }

           catch (Exception)
           {
               return Results.Problem("An error occurred while registering the vehicle.");
           }
       }).RequireAuthorization("Admin");
        return builder;
    }

}
