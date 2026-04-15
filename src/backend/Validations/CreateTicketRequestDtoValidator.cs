using FluentValidation;

namespace Project.DTOs;

public class CreateTicketRequestDtoValidator : AbstractValidator<CreateTicketRequestDto>
{
    public CreateTicketRequestDtoValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Ticket subject is required.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Ticket description is required.");

        RuleFor(x => x.TicketPriority)
            .IsInEnum()
            .WithMessage("Ticket priority is invalid.");
    }
}
