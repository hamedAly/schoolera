using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SupportTickets.Commands.ReplyToSupportTicketAsSupport;

public sealed class ReplyToSupportTicketAsSupportCommandValidator
    : AbstractValidator<ReplyToSupportTicketAsSupportCommand>
{
    public ReplyToSupportTicketAsSupportCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Body)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SupportTicketMessageBody);
    }
}
