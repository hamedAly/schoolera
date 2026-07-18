using FluentValidation;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Schools.Queries.GetPublicSchoolBySlug;

public sealed class GetPublicSchoolBySlugQueryValidator : AbstractValidator<GetPublicSchoolBySlugQuery>
{
    public GetPublicSchoolBySlugQueryValidator()
    {
        RuleFor(query => query.Slug)
            .NotEmpty()
            .Must(SlugHelper.IsValidSlug);
    }
}
