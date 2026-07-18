using FluentValidation;

namespace Schoolera.Application.Payments.Commands.MarkReconciliationInvestigating;

public sealed class MarkReconciliationInvestigatingCommandValidator
    : AbstractValidator<MarkReconciliationInvestigatingCommand>
{
    public MarkReconciliationInvestigatingCommandValidator()
    {
    }
}
