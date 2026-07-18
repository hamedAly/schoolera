using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.FinalizeEvaluationResult;

public sealed class FinalizeEvaluationResultCommandValidator
    : AbstractValidator<FinalizeEvaluationResultCommand>
{
    public FinalizeEvaluationResultCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.ResultRowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
