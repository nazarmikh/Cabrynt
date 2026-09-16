using Project.Services;

namespace Project.Endpoints;

public static class TripDurationModelEndpoints
{
    public static IEndpointRouteBuilder MapTripDurationModelEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/private/model-status", (ITripDurationModelStatusProvider modelStatusProvider) =>
            Results.Ok(modelStatusProvider.GetStatus()))
            .RequireAuthorization("Admin");

        return builder;
    }
}
