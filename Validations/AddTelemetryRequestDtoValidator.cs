using FluentValidation;

namespace Project.Validators;

public class AddTelemetryRequestDtoValidator : AbstractValidator<AddTelemetryRequestDto>
{
    public AddTelemetryRequestDtoValidator()
    {
        RuleFor(x => x.Latitude)
            .NotEmpty()
            .WithMessage("Latitude is required");

        RuleFor(x => x.Longitude)
            .NotEmpty()
            .WithMessage("Longitude is required");

        RuleFor(x => x.CurrentSpeed)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Speed cannot be negative");

        RuleFor(x => x.RemainingBatteryPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Battery percentage must be between 0 and 100");

        RuleFor(x => x.HardwareTemperature)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Hardware temperature cannot be negative");
    }
}
