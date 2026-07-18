using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.SetDefaultIntegration;

public sealed class SetDefaultIntegrationCommandValidator : AbstractValidator<SetDefaultIntegrationCommand>
{
    public SetDefaultIntegrationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
