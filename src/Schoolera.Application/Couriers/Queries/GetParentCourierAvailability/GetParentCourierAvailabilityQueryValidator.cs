using FluentValidation;

namespace Schoolera.Application.Couriers.Queries.GetParentCourierAvailability;

public sealed class GetParentCourierAvailabilityQueryValidator
    : AbstractValidator<GetParentCourierAvailabilityQuery>
{
    public GetParentCourierAvailabilityQueryValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x).Must(x => !x.CityId.HasValue || x.GovernorateId.HasValue);
        RuleFor(x => x).Must(x => !x.DistrictId.HasValue || x.CityId.HasValue);
    }
}
