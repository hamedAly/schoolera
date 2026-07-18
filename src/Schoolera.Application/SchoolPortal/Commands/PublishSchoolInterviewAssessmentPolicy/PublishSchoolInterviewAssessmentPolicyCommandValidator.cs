using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolInterviewAssessmentPolicy;

public sealed class PublishSchoolInterviewAssessmentPolicyCommandValidator
    : AbstractValidator<PublishSchoolInterviewAssessmentPolicyCommand>
{
    public PublishSchoolInterviewAssessmentPolicyCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.PolicyId).NotEmpty();
    }
}
