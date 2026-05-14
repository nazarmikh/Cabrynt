using Project.Services;
using FluentValidation;

namespace Project.Endpoints;

public static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/private/telemetry", async (
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
            return Results.Created();
        }).RequireAuthorization("Vehicle");

        return builder;
    }

}
