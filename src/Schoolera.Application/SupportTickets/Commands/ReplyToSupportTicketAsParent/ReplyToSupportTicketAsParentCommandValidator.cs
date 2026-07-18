using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SupportTickets.Commands.ReplyToSupportTicketAsParent;

public sealed class ReplyToSupportTicketAsParentCommandValidator
    : AbstractValidator<ReplyToSupportTicketAsParentCommand>
{
    public ReplyToSupportTicketAsParentCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Body)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SupportTicketMessageBody);
    }
}
