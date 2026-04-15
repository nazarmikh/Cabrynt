using FluentValidation;
using Project.Services;

namespace Project.Endpoints;

public static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/private/vehicles/{vehicleId:int}/maintenances", async (
            int vehicleId,
            CreateMaintenanceRequestDto request,
            IValidator<CreateMaintenanceRequestDto> validator,
            IMaintenanceService maintenanceService) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var response = await maintenanceService.AddMaintenanceAsync(vehicleId, request);
            if (response is null)
            {
                return Results.NotFound("Vehicle not found");
            }

            return Results.Created($"/api/private/vehicles/{vehicleId}/maintenances/{response.Id}", response);
        }).RequireAuthorization("Admin");

        builder.MapGet("/api/private/vehicles/{vehicleId:int}/maintenances", async (
            int vehicleId,
            IMaintenanceService maintenanceService) =>
        {
            var response = await maintenanceService.GetAllMaintenancesAsync(vehicleId);
            if (response is null)
            {
                return Results.NotFound("Vehicle not found");
            }

            return Results.Ok(response);
        }).RequireAuthorization("Admin");

        builder.MapGet("/api/private/vehicles/{vehicleId:int}/maintenances/{maintenanceId:int}", async (
            int vehicleId,
            int maintenanceId,
            IMaintenanceService maintenanceService) =>
        {
            var response = await maintenanceService.GetMaintenanceByIdAsync(vehicleId, maintenanceId);
            if (response is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        }).RequireAuthorization("Admin");

        return builder;
    }
}
