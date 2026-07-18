using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.TestIntegrationConnection;

public sealed class TestIntegrationConnectionCommandValidator : AbstractValidator<TestIntegrationConnectionCommand>
{
    public TestIntegrationConnectionCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
