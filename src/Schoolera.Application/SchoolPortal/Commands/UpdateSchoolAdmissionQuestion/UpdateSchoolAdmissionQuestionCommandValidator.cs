using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdmissionQuestion;

public sealed class UpdateSchoolAdmissionQuestionCommandValidator
    : AbstractValidator<UpdateSchoolAdmissionQuestionCommand>
{
    public UpdateSchoolAdmissionQuestionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.LabelAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.LabelEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.HelpAr).MaximumLength(1000);
        RuleFor(x => x.Body.HelpEn).MaximumLength(1000);
        RuleFor(x => x.Body.SortOrder).GreaterThanOrEqualTo(0);
    }
}
