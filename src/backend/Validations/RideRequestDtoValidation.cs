using FluentValidation;
using Project.Services;

namespace Project.DTOs;

public class RideRequestDtoValidation : AbstractValidator<RideRequestDto>
{
    public RideRequestDtoValidation()
    {
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

        RuleFor(x => x.PreferredServiceTier)
            .IsInEnum()
            .WithMessage("Service tier is invalid.");

        RuleFor(x => x)
            .Must(x =>
                PortoServiceArea.Contains(x.DepartureLatitude, x.DepartureLongitude)
                && PortoServiceArea.Contains(x.DestinationLatitude, x.DestinationLongitude))
            .WithMessage("Cabrynt currently supports routes inside the Porto service area.");

        RuleFor(x => x)
            .Must(x =>
                x.DepartureLatitude != x.DestinationLatitude
                || x.DepartureLongitude != x.DestinationLongitude)
            .WithMessage("Pickup and destination coordinates cannot be identical.");
    }

}
