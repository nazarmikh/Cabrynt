using FluentValidation;

namespace Project.Validators;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(e => e.Email)
        .MaximumLength(254)
        .EmailAddress()
        .NotEmpty();

        RuleFor(p => p.Password)
        .MaximumLength(128)
        .MinimumLength(8)
        .NotEmpty();
    }
    
}