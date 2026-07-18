using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CloneSchoolInterviewAssessmentPolicy;

public sealed class CloneSchoolInterviewAssessmentPolicyCommandValidator
    : AbstractValidator<CloneSchoolInterviewAssessmentPolicyCommand>
{
    public CloneSchoolInterviewAssessmentPolicyCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.PolicyId).NotEmpty();
    }
}
