using FluentValidation;
using Project.Services;

namespace Project.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/private/rides/{rideId}/payments", async 
        (int rideId,
        IPaymentService paymentService,
        CreatePaymentRequestDto request,
        IValidator<CreatePaymentRequestDto> validator) =>
        {
            if (request.RideId != rideId)
            {
                return Results.BadRequest("Ride id in route and body must match");
            }

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            try
            {
               var response = await paymentService.CreatePaymentAsync(request);
                if (response is null)
                {
                    return Results.BadRequest("Ride must be completed before payment can be created");
                }
                return Results.Created($"/api/private/rides/payments/{response.Id}", response); 
            }
            catch (InvalidOperationException exception) when (exception.Message == "Ride not found")
            {
                return Results.NotFound(exception.Message);
            }
            catch (Exception)
            {
                return Results.Problem("An unexpected error occurred while processing the payment");
            }
        }).RequireAuthorization("AdminOrVehicle");

        return builder;
    }
}
