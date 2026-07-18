using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.UpdateIntegration;

public sealed class UpdateIntegrationCommandValidator : AbstractValidator<UpdateIntegrationCommand>
{
    public UpdateIntegrationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.ProviderCode).NotEmpty();
        RuleFor(command => command.Body.DisplayNameAr).NotEmpty();
        RuleFor(command => command.Body.SettingsJson).NotNull();
    }
}
