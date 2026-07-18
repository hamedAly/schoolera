using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Taxonomies.Dtos;
using Schoolera.Application.Taxonomies.Queries.GetAcademicYears;
using Schoolera.Application.Taxonomies.Queries.GetCities;
using Schoolera.Application.Taxonomies.Queries.GetCitiesByGovernorate;
using Schoolera.Application.Taxonomies.Queries.GetCountries;
using Schoolera.Application.Taxonomies.Queries.GetCurricula;
using Schoolera.Application.Taxonomies.Queries.GetDistrictsByCity;
using Schoolera.Application.Taxonomies.Queries.GetEducationalStages;
using Schoolera.Application.Taxonomies.Queries.GetFacilities;
using Schoolera.Application.Taxonomies.Queries.GetGovernoratesByCountry;
using Schoolera.Application.Taxonomies.Queries.GetGradesByStage;

namespace Schoolera.Api.Controllers;

[Route("api/taxonomies")]
public sealed class TaxonomiesController(
    ISender mediator,
    ILogger<TaxonomiesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("countries")]
    public async Task<Result<IReadOnlyList<CountryTaxonomyItemDto>>> GetCountries(
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetCountriesQuery(), cancellationToken);
        return Success(items);
    }

    [HttpGet("countries/{countryId:guid}/governorates")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetGovernoratesByCountry(
        Guid countryId,
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetGovernoratesByCountryQuery(countryId), cancellationToken);
        return Success(items);
    }

    [HttpGet("governorates/{governorateId:guid}/cities")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetCitiesByGovernorate(
        Guid governorateId,
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetCitiesByGovernorateQuery(governorateId), cancellationToken);
        return Success(items);
    }

    [HttpGet("cities")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetCities(
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetCitiesQuery(), cancellationToken);
        return Success(items);
    }

    [HttpGet("cities/{cityId:guid}/districts")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetDistrictsByCity(
        Guid cityId,
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetDistrictsByCityQuery(cityId), cancellationToken);
        return Success(items);
    }

    [HttpGet("curricula")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetCurricula(
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetCurriculaQuery(), cancellationToken);
        return Success(items);
    }

    [HttpGet("educational-stages")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetEducationalStages(
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetEducationalStagesQuery(), cancellationToken);
        return Success(items);
    }

    [HttpGet("educational-stages/{stageId:guid}/grades")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetGradesByStage(
        Guid stageId,
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetGradesByStageQuery(stageId), cancellationToken);
        return Success(items);
    }

    [HttpGet("facilities")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetFacilities(
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetFacilitiesQuery(), cancellationToken);
        return Success(items);
    }

    [HttpGet("academic-years")]
    public async Task<Result<IReadOnlyList<TaxonomyItemDto>>> GetAcademicYears(
        CancellationToken cancellationToken)
    {
        var items = await Mediator.Send(new GetAcademicYearsQuery(), cancellationToken);
        return Success(items);
    }
}
