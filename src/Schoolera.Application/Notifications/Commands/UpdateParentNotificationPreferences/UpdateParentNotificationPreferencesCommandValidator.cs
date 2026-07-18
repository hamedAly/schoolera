using FluentValidation;

namespace Schoolera.Application.Notifications.Commands.UpdateParentNotificationPreferences;

public sealed class UpdateParentNotificationPreferencesCommandValidator : AbstractValidator<UpdateParentNotificationPreferencesCommand>
{
    public UpdateParentNotificationPreferencesCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
    }
}
