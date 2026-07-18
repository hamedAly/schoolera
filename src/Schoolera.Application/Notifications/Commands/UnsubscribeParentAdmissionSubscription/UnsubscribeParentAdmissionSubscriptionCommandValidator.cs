using FluentValidation;

namespace Schoolera.Application.Notifications.Commands.UnsubscribeParentAdmissionSubscription;

public sealed class UnsubscribeParentAdmissionSubscriptionCommandValidator : AbstractValidator<UnsubscribeParentAdmissionSubscriptionCommand>
{
    public UnsubscribeParentAdmissionSubscriptionCommandValidator()
    {
        RuleFor(command => command.SubscriptionId).NotEmpty();
    }
}
