using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.ReassignSupportTicket;

public sealed class ReassignSupportTicketCommandValidator : AbstractValidator<ReassignSupportTicketCommand>
{
    public ReassignSupportTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.SupportAgentUserId).NotEmpty();
    }
}
