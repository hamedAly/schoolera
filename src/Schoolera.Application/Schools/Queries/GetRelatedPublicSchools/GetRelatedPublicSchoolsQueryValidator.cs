using FluentValidation;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Schools.Queries.GetRelatedPublicSchools;

public sealed class GetRelatedPublicSchoolsQueryValidator : AbstractValidator<GetRelatedPublicSchoolsQuery>
{
    public GetRelatedPublicSchoolsQueryValidator()
    {
        RuleFor(query => query.Slug)
            .NotEmpty()
            .Must(SlugHelper.IsValidSlug);

        RuleFor(query => query.Limit)
            .Must(limit => limit is null or (>= 1 and <= 8))
            .WithMessage("Limit must be between 1 and 8.");
    }
}
