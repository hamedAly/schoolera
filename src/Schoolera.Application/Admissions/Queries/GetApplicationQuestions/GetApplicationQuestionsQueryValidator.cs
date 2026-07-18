using FluentValidation;

namespace Schoolera.Application.Admissions.Queries.GetApplicationQuestions;

public sealed class GetApplicationQuestionsQueryValidator
    : AbstractValidator<GetApplicationQuestionsQuery>
{
    public GetApplicationQuestionsQueryValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
