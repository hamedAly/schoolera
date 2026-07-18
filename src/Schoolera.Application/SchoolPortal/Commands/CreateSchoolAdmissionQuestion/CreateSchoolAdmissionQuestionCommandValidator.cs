using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdmissionQuestion;

public sealed class CreateSchoolAdmissionQuestionCommandValidator
    : AbstractValidator<CreateSchoolAdmissionQuestionCommand>
{
    public CreateSchoolAdmissionQuestionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.QuestionCode).NotEmpty().MaximumLength(64)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$");
        RuleFor(x => x.Body.LabelAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.LabelEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.HelpAr).MaximumLength(1000);
        RuleFor(x => x.Body.HelpEn).MaximumLength(1000);
        RuleFor(x => x.Body.QuestionType).IsInEnum();
        RuleFor(x => x.Body.SortOrder).GreaterThanOrEqualTo(0);
    }
}
