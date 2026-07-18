using FluentValidation;

namespace Schoolera.Application.Payments.Commands.ProcessSandboxPaymentWebhook;

public sealed class ProcessSandboxPaymentWebhookCommandValidator : AbstractValidator<ProcessSandboxPaymentWebhookCommand>
{
    public ProcessSandboxPaymentWebhookCommandValidator()
    {
    }
}
