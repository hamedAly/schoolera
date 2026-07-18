using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolInterviewAssessmentPolicy;

public sealed class UnpublishSchoolInterviewAssessmentPolicyCommandValidator
    : AbstractValidator<UnpublishSchoolInterviewAssessmentPolicyCommand>
{
    public UnpublishSchoolInterviewAssessmentPolicyCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.PolicyId).NotEmpty();
    }
}
