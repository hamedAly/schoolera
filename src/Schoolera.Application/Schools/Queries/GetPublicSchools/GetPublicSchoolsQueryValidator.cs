using FluentValidation;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Schools.Queries.GetPublicSchools;

public sealed class GetPublicSchoolsQueryValidator : AbstractValidator<GetPublicSchoolsQuery>
{
    public GetPublicSchoolsQueryValidator(ITaxonomyRepository taxonomyRepository)
    {
        RuleFor(query => query.Search)
            .MaximumLength(FieldLengthLimits.SchoolName)
            .When(query => !string.IsNullOrWhiteSpace(query.Search));

        RuleFor(query => query.Sort)
            .Must(value => PublicSchoolSortParser.TryParse(value, out _))
            .WithMessage("Sort must be one of: relevance, name-asc, name-desc, lowest-fee, highest-fee, newest, nearest.")
            .When(query => !string.IsNullOrWhiteSpace(query.Sort));

        RuleFor(query => query.MinimumTuition)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MinimumTuition.HasValue);

        RuleFor(query => query.MaximumTuition)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MaximumTuition.HasValue);

        RuleFor(query => query)
            .Must(query =>
                !query.MinimumTuition.HasValue
                || !query.MaximumTuition.HasValue
                || query.MinimumTuition <= query.MaximumTuition)
            .WithMessage("MinimumTuition must not exceed MaximumTuition.");

        RuleFor(query => query.Latitude)
            .InclusiveBetween(-90, 90)
            .When(query => query.Latitude.HasValue);

        RuleFor(query => query.Longitude)
            .InclusiveBetween(-180, 180)
            .When(query => query.Longitude.HasValue);

        RuleFor(query => query)
            .Must(query =>
            {
                PublicSchoolSortParser.TryParse(query.Sort, out var sort);
                if (sort != PublicSchoolSort.Nearest)
                {
                    return true;
                }

                return query.Latitude is >= -90 and <= 90
                    && query.Longitude is >= -180 and <= 180;
            })
            .WithMessage("Nearest sort requires valid Latitude and Longitude.");

        RuleFor(query => query)
            .MustAsync(async (query, cancellationToken) =>
            {
                if (query.CityId is null || query.DistrictId is null)
                {
                    return true;
                }

                var district = await taxonomyRepository.GetDistrictByIdAsync(
                    query.DistrictId.Value,
                    cancellationToken);
                return district is not null && district.CityId == query.CityId;
            })
            .WithMessage("DistrictId must belong to the selected CityId.");

        RuleFor(query => query)
            .MustAsync(async (query, cancellationToken) =>
            {
                if (query.CityId is null || query.GovernorateId is null)
                {
                    return true;
                }

                var city = await taxonomyRepository.GetCityByIdAsync(
                    query.CityId.Value,
                    cancellationToken);
                return city is not null && city.GovernorateId == query.GovernorateId;
            })
            .WithMessage("CityId must belong to the selected GovernorateId.");

        RuleFor(query => query)
            .MustAsync(async (query, cancellationToken) =>
            {
                if (query.GovernorateId is null || query.CountryId is null)
                {
                    return true;
                }

                var governorate = await taxonomyRepository.GetGovernorateByIdAsync(
                    query.GovernorateId.Value,
                    cancellationToken);
                return governorate is not null && governorate.CountryId == query.CountryId;
            })
            .WithMessage("GovernorateId must belong to the selected CountryId.");

        RuleFor(query => query)
            .MustAsync(async (query, cancellationToken) =>
            {
                if (query.CityId is null || query.CountryId is null)
                {
                    return true;
                }

                var city = await taxonomyRepository.GetCityByIdAsync(
                    query.CityId.Value,
                    cancellationToken);
                if (city?.GovernorateId is null)
                {
                    return false;
                }

                var governorate = await taxonomyRepository.GetGovernorateByIdAsync(
                    city.GovernorateId.Value,
                    cancellationToken);
                return governorate is not null && governorate.CountryId == query.CountryId;
            })
            .WithMessage("CityId must belong to a governorate in the selected CountryId.");

        RuleFor(query => query)
            .MustAsync(async (query, cancellationToken) =>
            {
                if (query.StageId is null || query.GradeId is null)
                {
                    return true;
                }

                var grade = await taxonomyRepository.GetGradeByIdAsync(
                    query.GradeId.Value,
                    cancellationToken);
                return grade is not null && grade.EducationalStageId == query.StageId;
            })
            .WithMessage("GradeId must belong to the selected EducationalStageId (stageId).");
    }
}
