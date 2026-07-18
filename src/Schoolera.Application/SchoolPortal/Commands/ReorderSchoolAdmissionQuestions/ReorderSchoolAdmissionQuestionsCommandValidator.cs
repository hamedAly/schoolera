using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdmissionQuestions;

public sealed class ReorderSchoolAdmissionQuestionsCommandValidator
    : AbstractValidator<ReorderSchoolAdmissionQuestionsCommand>
{
    public ReorderSchoolAdmissionQuestionsCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.OrderedQuestionIds).NotEmpty();
    }
}
