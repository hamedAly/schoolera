using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.BeginEvaluationCorrection;

public sealed class BeginEvaluationCorrectionCommandValidator
    : AbstractValidator<BeginEvaluationCorrectionCommand>
{
    public BeginEvaluationCorrectionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.CorrectionReason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body.ResultRowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
