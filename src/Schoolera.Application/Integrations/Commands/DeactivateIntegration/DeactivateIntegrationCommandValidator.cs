using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.DeactivateIntegration;

public sealed class DeactivateIntegrationCommandValidator : AbstractValidator<DeactivateIntegrationCommand>
{
    public DeactivateIntegrationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
