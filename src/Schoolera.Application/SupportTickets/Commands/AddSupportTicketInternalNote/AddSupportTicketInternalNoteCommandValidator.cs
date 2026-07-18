using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SupportTickets.Commands.AddSupportTicketInternalNote;

public sealed class AddSupportTicketInternalNoteCommandValidator
    : AbstractValidator<AddSupportTicketInternalNoteCommand>
{
    public AddSupportTicketInternalNoteCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Body)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SupportTicketMessageBody);
    }
}
