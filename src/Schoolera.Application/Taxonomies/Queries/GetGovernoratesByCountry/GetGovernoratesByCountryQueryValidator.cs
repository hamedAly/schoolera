using FluentValidation;

namespace Schoolera.Application.Taxonomies.Queries.GetGovernoratesByCountry;

public sealed class GetGovernoratesByCountryQueryValidator : AbstractValidator<GetGovernoratesByCountryQuery>
{
    public GetGovernoratesByCountryQueryValidator()
    {
        RuleFor(query => query.CountryId)
            .NotEmpty();
    }
}
