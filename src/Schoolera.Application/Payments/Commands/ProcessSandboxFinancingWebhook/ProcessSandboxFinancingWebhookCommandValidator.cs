using FluentValidation;

namespace Schoolera.Application.Payments.Commands.ProcessSandboxFinancingWebhook;

public sealed class ProcessSandboxFinancingWebhookCommandValidator
    : AbstractValidator<ProcessSandboxFinancingWebhookCommand>
{
    public ProcessSandboxFinancingWebhookCommandValidator()
    {
    }
}
