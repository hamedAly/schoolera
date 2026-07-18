using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolInterviewAssessmentPolicy;

public sealed class DeactivateSchoolInterviewAssessmentPolicyCommandValidator
    : AbstractValidator<DeactivateSchoolInterviewAssessmentPolicyCommand>
{
    public DeactivateSchoolInterviewAssessmentPolicyCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.PolicyId).NotEmpty();
    }
}
