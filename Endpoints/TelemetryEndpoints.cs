using Project.Services;
using FluentValidation;

namespace Project.Endpoints;

public static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/telemetry", async (
            AddTelemetryRequestDto request, 
            ITelemetryService telemetryService,
            IValidator<AddTelemetryRequestDto> validator) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }


            await telemetryService.AddTelemetryAsync(request);
            return Results.Ok();
        }).RequireAuthorization("Telemetry");

        return builder;
    }

} 
