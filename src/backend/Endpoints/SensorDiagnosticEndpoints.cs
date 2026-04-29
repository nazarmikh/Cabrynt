using FluentValidation;
using Project.Services;

namespace Project.Endpoints;

public static class SensorDiagnosticEndpoints
{
    public static IEndpointRouteBuilder MapSensorDiagnosticEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/private/sensor-diagnostics", async (
            CreateSensorDiagnosticRequestDto request,
            IValidator<CreateSensorDiagnosticRequestDto> validator,
            ISensorDiagnosticService sensorDiagnosticService) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            await sensorDiagnosticService.AddSensorDiagnosticAsync(request);
            return Results.Created();
        }).RequireAuthorization("Vehicle");

        return builder;
    }
}
