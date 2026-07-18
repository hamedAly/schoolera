using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.AssignSupportTicket;

public sealed class AssignSupportTicketCommandValidator : AbstractValidator<AssignSupportTicketCommand>
{
    public AssignSupportTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.SupportAgentUserId).NotEmpty();
    }
}
