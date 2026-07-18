using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketStatus;

public sealed class ChangeSupportTicketStatusCommandValidator : AbstractValidator<ChangeSupportTicketStatusCommand>
{
    public ChangeSupportTicketStatusCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Status).GreaterThan(0);
    }
}
