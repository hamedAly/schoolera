using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.CreateIntegration;

public sealed class CreateIntegrationCommandValidator : AbstractValidator<CreateIntegrationCommand>
{
    public CreateIntegrationCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.ProviderCode).NotEmpty();
        RuleFor(command => command.Body.DisplayNameAr).NotEmpty();
        RuleFor(command => command.Body.SettingsJson).NotNull();
        RuleFor(command => command.Body.IntegrationType).IsInEnum();
    }
}
