using FluentValidation;

namespace Schoolera.Application.Taxonomies.Queries.GetGradesByStage;

public sealed class GetGradesByStageQueryValidator : AbstractValidator<GetGradesByStageQuery>
{
    public GetGradesByStageQueryValidator()
    {
        RuleFor(query => query.StageId)
            .NotEmpty();
    }
}
