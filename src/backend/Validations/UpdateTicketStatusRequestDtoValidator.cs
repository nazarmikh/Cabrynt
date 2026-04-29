using FluentValidation;

namespace Project.DTOs;

public class UpdateTicketStatusRequestDtoValidator : AbstractValidator<UpdateTicketStatusRequestDto>
{
    public UpdateTicketStatusRequestDtoValidator()
    {
        RuleFor(x => x.TicketStatus)
            .IsInEnum();
    }
}
