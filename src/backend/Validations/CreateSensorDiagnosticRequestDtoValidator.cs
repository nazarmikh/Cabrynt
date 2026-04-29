using FluentValidation;

namespace Project.Validations;

public class CreateSensorDiagnosticRequestDtoValidator : AbstractValidator<CreateSensorDiagnosticRequestDto>
{
    public CreateSensorDiagnosticRequestDtoValidator()
    {
        RuleFor(x => x.SensorType)
            .IsInEnum();

        RuleFor(x => x.ErrorCode)
            .GreaterThan(0);

        RuleFor(x => x.DeviationSeverity)
            .IsInEnum();

        RuleFor(x => x.RawSensorValue)
            .NotEmpty()
            .MaximumLength(10000);

        RuleFor(x => x.VehicleTelemetryId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.VehicleId)
            .GreaterThan(0);
    }
}
