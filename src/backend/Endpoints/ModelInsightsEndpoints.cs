using FluentValidation;
using Project.Services;

namespace Project.Endpoints;

public static class ModelInsightsEndpoints
{
    public static IEndpointRouteBuilder MapModelInsightsEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/public/model-insights/estimate", async (
            RideRequestDto request,
            IValidator<RideRequestDto> validator,
            IQuoteService quoteService,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).ToArray()));
            }

            try
            {
                var quote = await quoteService.CalculateAsync(
                    request,
                    DateTimeOffset.UtcNow,
                    cancellationToken);

                return Results.Ok(new ModelDemoEstimateResponseDto
                {
                    RouteDistance = quote.Breakdown.Distance,
                    RouteDuration = quote.Breakdown.Duration,
                    RouteEstimateSource = quote.RouteEstimateSource,
                    EstimatedTripDuration = quote.EstimatedTripDuration,
                    EstimatedTripDurationSource = quote.EstimatedTripDurationSource,
                    ModelCorrection = quote.EstimatedTripDurationSource == TripDurationEstimateSource.MachineLearning
                        ? quote.EstimatedTripDuration - quote.Breakdown.Duration
                        : null,
                    ModelVersion = quote.TripDurationModelVersion
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { message = exception.Message });
            }
            catch (Exception)
            {
                return Results.Problem("Failed to calculate the model demonstration estimate.");
            }
        })
        .RequireRateLimiting("model-demo");

        return builder;
    }
}
