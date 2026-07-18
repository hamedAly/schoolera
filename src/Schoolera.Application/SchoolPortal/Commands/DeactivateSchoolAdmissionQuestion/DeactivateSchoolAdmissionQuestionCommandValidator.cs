using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolAdmissionQuestion;

public sealed class DeactivateSchoolAdmissionQuestionCommandValidator
    : AbstractValidator<DeactivateSchoolAdmissionQuestionCommand>
{
    public DeactivateSchoolAdmissionQuestionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
