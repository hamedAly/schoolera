using FluentValidation;

namespace Schoolera.Application.Notifications.Commands.CreateParentAdmissionSubscription;

public sealed class CreateParentAdmissionSubscriptionCommandValidator : AbstractValidator<CreateParentAdmissionSubscriptionCommand>
{
    public CreateParentAdmissionSubscriptionCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.SchoolId).NotEmpty();
        RuleFor(command => command.Body.PreferredChannel).IsInEnum();
    }
}
