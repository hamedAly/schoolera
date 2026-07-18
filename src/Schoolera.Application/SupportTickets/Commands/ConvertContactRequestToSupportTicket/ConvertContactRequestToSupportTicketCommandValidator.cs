using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.ConvertContactRequestToSupportTicket;

public sealed class ConvertContactRequestToSupportTicketCommandValidator
    : AbstractValidator<ConvertContactRequestToSupportTicketCommand>
{
    public ConvertContactRequestToSupportTicketCommandValidator()
    {
        RuleFor(command => command.ContactRequestId).NotEmpty();
    }
}
