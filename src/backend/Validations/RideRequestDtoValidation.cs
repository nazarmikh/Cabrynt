using FluentValidation;

namespace Project.DTOs;

public class RideRequestDtoValidation : AbstractValidator<RideRequestDto>
{
    public RideRequestDtoValidation()
    {
        RuleFor(x => x.DepartureLocation)
            .NotEmpty()
            .WithMessage("Pickup location is required.");

        RuleFor(x => x.DestinationLocation)
            .NotEmpty()
            .WithMessage("Destination is required.");
    }

}