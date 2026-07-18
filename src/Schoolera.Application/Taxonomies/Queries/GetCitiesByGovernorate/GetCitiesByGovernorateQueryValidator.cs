using FluentValidation;

namespace Schoolera.Application.Taxonomies.Queries.GetCitiesByGovernorate;

public sealed class GetCitiesByGovernorateQueryValidator : AbstractValidator<GetCitiesByGovernorateQuery>
{
    public GetCitiesByGovernorateQueryValidator()
    {
        RuleFor(query => query.GovernorateId)
            .NotEmpty();
    }
}
