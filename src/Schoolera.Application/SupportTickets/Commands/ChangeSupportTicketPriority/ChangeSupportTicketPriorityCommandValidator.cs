using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketPriority;

public sealed class ChangeSupportTicketPriorityCommandValidator
    : AbstractValidator<ChangeSupportTicketPriorityCommand>
{
    public ChangeSupportTicketPriorityCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Priority).GreaterThan(0);
    }
}
