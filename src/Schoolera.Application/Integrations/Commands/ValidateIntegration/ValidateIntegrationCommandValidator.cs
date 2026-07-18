using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.ValidateIntegration;

public sealed class ValidateIntegrationCommandValidator : AbstractValidator<ValidateIntegrationCommand>
{
    public ValidateIntegrationCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.SettingsJson).NotNull();
        RuleFor(command => command.Id)
            .NotEmpty()
            .When(command => command.Id is not null);
    }
}
