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

        RuleFor(x => x.DepartureLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Pickup latitude is out of bounds.");

        RuleFor(x => x.DepartureLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Pickup longitude is out of bounds.");

        RuleFor(x => x.DestinationLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Destination latitude is out of bounds.");

        RuleFor(x => x.DestinationLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Destination longitude is out of bounds.");
    }

}