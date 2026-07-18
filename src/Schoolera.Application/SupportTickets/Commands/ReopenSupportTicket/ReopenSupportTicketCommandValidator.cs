using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.ReopenSupportTicket;

public sealed class ReopenSupportTicketCommandValidator : AbstractValidator<ReopenSupportTicketCommand>
{
    public ReopenSupportTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
    }
}
