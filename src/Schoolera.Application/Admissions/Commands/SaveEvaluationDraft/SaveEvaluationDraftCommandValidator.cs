using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.SaveEvaluationDraft;

public sealed class SaveEvaluationDraftCommandValidator : AbstractValidator<SaveEvaluationDraftCommand>
{
    public SaveEvaluationDraftCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.ChildAttendance).IsInEnum();
        RuleFor(x => x.Body.ParentAttendance).IsInEnum();
        RuleFor(x => x.Body.Recommendation).IsInEnum();
        RuleFor(x => x.Body.SuggestedParentReasonAr).MaximumLength(2000);
        RuleFor(x => x.Body.SuggestedParentReasonEn).MaximumLength(2000);
        RuleFor(x => x.Body.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.Body.RowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Body.Answers).NotNull().Must(x => x.Count <= 100);
    }
}
