using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.ActivateIntegration;

public sealed class ActivateIntegrationCommandValidator : AbstractValidator<ActivateIntegrationCommand>
{
    public ActivateIntegrationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
