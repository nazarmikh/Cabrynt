using FluentValidation;

namespace Project.Validations;

public class CreateMaintenanceRequestDtoValidator : AbstractValidator<CreateMaintenanceRequestDto>
{
    public CreateMaintenanceRequestDtoValidator()
    {
        RuleFor(x => x.ServiceDate)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.TechnicianName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.NextInspectionMileage)
            .GreaterThan(0);
    }
}
