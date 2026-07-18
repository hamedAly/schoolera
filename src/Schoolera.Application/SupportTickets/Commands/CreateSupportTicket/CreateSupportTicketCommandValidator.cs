using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SupportTickets.Commands.CreateSupportTicket;

public sealed class CreateSupportTicketCommandValidator : AbstractValidator<CreateSupportTicketCommand>
{
    public CreateSupportTicketCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Subject)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SupportTicketSubject);
        RuleFor(command => command.Body.Body)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SupportTicketMessageBody);
        RuleFor(command => command.Body.Category).GreaterThan(0);
        RuleFor(command => command.Body.Priority).GreaterThan(0);
    }
}
