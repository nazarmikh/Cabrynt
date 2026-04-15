using FluentValidation;

namespace Project.Validations;

public class CreatePaymentRequestDtoValidator : AbstractValidator<CreatePaymentRequestDto>
{
    public CreatePaymentRequestDtoValidator()
    {
        RuleFor(x => x.RideId)
            .GreaterThan(0);
    }
}
