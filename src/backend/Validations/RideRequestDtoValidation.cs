using FluentValidation;

namespace Project.DTOs;

public class RideRequestDtoValidation : AbstractValidator<RideRequestDto>
{
    public RideRequestDtoValidation()
    {
        RuleFor(x => x.DepartureLocation)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Pickup location is required.");

        RuleFor(x => x.DestinationLocation)
            .NotEmpty()
            .MaximumLength(200)
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

        RuleFor(x => x.DiscountCode)
            .MaximumLength(50);

        RuleFor(x => x.DiscountCode)
            .Must(code => string.IsNullOrWhiteSpace(code) || code.Trim().Length == code.Length)
            .WithMessage("Discount code cannot start or end with spaces.");

        RuleFor(x => x.PreferredVehicleType)
            .IsInEnum()
            .WithMessage("Vehicle type is invalid.");

        RuleFor(x => x)
            .Must(x =>
                x.DepartureLatitude != x.DestinationLatitude
                || x.DepartureLongitude != x.DestinationLongitude)
            .WithMessage("Pickup and destination coordinates cannot be identical.");
    }

}
