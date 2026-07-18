using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolAdmissionQuestion;

public sealed class PublishSchoolAdmissionQuestionCommandValidator
    : AbstractValidator<PublishSchoolAdmissionQuestionCommand>
{
    public PublishSchoolAdmissionQuestionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
