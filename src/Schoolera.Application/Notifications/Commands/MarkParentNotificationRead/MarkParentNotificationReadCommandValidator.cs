using FluentValidation;

namespace Schoolera.Application.Notifications.Commands.MarkParentNotificationRead;

public sealed class MarkParentNotificationReadCommandValidator : AbstractValidator<MarkParentNotificationReadCommand>
{
    public MarkParentNotificationReadCommandValidator()
    {
        RuleFor(command => command.NotificationId).NotEmpty();
    }
}
