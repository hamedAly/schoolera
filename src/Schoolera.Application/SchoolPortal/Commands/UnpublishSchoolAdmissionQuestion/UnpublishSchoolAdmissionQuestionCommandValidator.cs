using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolAdmissionQuestion;

public sealed class UnpublishSchoolAdmissionQuestionCommandValidator
    : AbstractValidator<UnpublishSchoolAdmissionQuestionCommand>
{
    public UnpublishSchoolAdmissionQuestionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
