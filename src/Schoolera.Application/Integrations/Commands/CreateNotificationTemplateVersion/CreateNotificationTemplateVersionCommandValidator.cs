using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.CreateNotificationTemplateVersion;

public sealed class CreateNotificationTemplateVersionCommandValidator : AbstractValidator<CreateNotificationTemplateVersionCommand>
{
    public CreateNotificationTemplateVersionCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Body).NotEmpty();
        RuleFor(command => command.Body.Culture).NotEmpty();
        RuleFor(command => command.Body.EventType).IsInEnum();
        RuleFor(command => command.Body.Channel).IsInEnum();
        RuleFor(command => command.Body.AllowedVariablesCsv).NotNull();
    }
}
