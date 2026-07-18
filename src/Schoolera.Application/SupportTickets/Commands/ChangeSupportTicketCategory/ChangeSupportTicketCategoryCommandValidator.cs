using FluentValidation;

namespace Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketCategory;

public sealed class ChangeSupportTicketCategoryCommandValidator
    : AbstractValidator<ChangeSupportTicketCategoryCommand>
{
    public ChangeSupportTicketCategoryCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Category).GreaterThan(0);
    }
}
