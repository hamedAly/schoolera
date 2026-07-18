using FluentValidation;

namespace Schoolera.Application.Taxonomies.Queries.GetDistrictsByCity;

public sealed class GetDistrictsByCityQueryValidator : AbstractValidator<GetDistrictsByCityQuery>
{
    public GetDistrictsByCityQueryValidator()
    {
        RuleFor(query => query.CityId)
            .NotEmpty();
    }
}
