using FluentValidation;

namespace Schoolera.Application.Notifications.Commands.MarkAllParentNotificationsRead;

public sealed class MarkAllParentNotificationsReadCommandValidator : AbstractValidator<MarkAllParentNotificationsReadCommand>
{
    public MarkAllParentNotificationsReadCommandValidator()
    {
    }
}
