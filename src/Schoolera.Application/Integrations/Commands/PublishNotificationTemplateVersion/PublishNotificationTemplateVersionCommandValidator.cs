using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.PublishNotificationTemplateVersion;

public sealed class PublishNotificationTemplateVersionCommandValidator : AbstractValidator<PublishNotificationTemplateVersionCommand>
{
    public PublishNotificationTemplateVersionCommandValidator()
    {
        RuleFor(command => command.VersionId).NotEmpty();
    }
}
