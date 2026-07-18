using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.AssignSupportTicketToMe;

public sealed class AssignSupportTicketToMeCommandValidator : AbstractValidator<AssignSupportTicketToMeCommand>
{
    public AssignSupportTicketToMeCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
    }
}
