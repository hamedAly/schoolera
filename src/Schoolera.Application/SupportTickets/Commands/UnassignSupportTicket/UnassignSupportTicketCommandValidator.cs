using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.UnassignSupportTicket;

public sealed class UnassignSupportTicketCommandValidator : AbstractValidator<UnassignSupportTicketCommand>
{
    public UnassignSupportTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
    }
}
