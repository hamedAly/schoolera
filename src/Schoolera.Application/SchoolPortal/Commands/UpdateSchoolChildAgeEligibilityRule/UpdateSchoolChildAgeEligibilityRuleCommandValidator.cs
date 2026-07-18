using FluentValidation;
using Schoolera.Application.Admissions.Common;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolChildAgeEligibilityRule;

public sealed class UpdateSchoolChildAgeEligibilityRuleCommandValidator
    : AbstractValidator<UpdateSchoolChildAgeEligibilityRuleCommand>
{
    public UpdateSchoolChildAgeEligibilityRuleCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.RuleId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.EducationalStageId).NotEmpty();
        RuleFor(x => x.Body.AcademicYearId).NotEmpty();
        RuleFor(x => x.Body.ReferenceDateMode).IsInEnum();
        RuleFor(x => x.Body.MinAgeCompletedMonths)
            .InclusiveBetween(ChildAgeEligibilityCatalog.MinCompletedMonths, ChildAgeEligibilityCatalog.MaxCompletedMonths);
        RuleFor(x => x.Body.MaxAgeCompletedMonths)
            .InclusiveBetween(ChildAgeEligibilityCatalog.MinCompletedMonths, ChildAgeEligibilityCatalog.MaxCompletedMonths)
            .GreaterThanOrEqualTo(x => x.Body.MinAgeCompletedMonths);
        RuleFor(x => x.Body.ExplanationAr).MaximumLength(FieldLengthLimits.AgeEligibilityExplanation);
        RuleFor(x => x.Body.ExplanationEn).MaximumLength(FieldLengthLimits.AgeEligibilityExplanation);
    }
}
