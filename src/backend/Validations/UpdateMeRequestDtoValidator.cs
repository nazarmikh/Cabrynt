using FluentValidation;

namespace Project.DTOs;

public class UpdateMeRequestDtoValidator : AbstractValidator<UpdateMeRequestDto>
{
    public UpdateMeRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(254);

        RuleFor(x => x.HomeAddress)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.PreferredPaymentMethod)
            .IsInEnum();
    }
}
