using FluentValidation;

namespace Schoolera.Application.Payments.Commands.ResolveReconciliation;

public sealed class ResolveReconciliationCommandValidator : AbstractValidator<ResolveReconciliationCommand>
{
    public ResolveReconciliationCommandValidator()
    {
    }
}
