using FluentValidation;
namespace Project.Validators;

public class RegisterVehicleRequestDtoValidation : AbstractValidator<RegisterVehicleRequestDto>
{
    public RegisterVehicleRequestDtoValidation()
    {
        RuleFor(v => v.VIN)
            .NotEmpty()
            .MaximumLength(17)
            .MinimumLength(17);

        RuleFor(v => v.LicencePlate)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(v => v.Model)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(v => v.Year)
            .InclusiveBetween(1970, DateTime.Now.Year);
    }

}