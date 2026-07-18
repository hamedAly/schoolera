using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateAcademicYear;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateCity;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateCountry;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateCurriculum;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateDistrict;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateEducationalStage;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateFacility;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateGovernorate;
using Schoolera.Application.Admin.Taxonomies.Commands.CreateGrade;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateAcademicYear;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCity;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCountry;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCurriculum;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateDistrict;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateEducationalStage;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateFacility;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateGovernorate;
using Schoolera.Application.Admin.Taxonomies.Commands.DeactivateGrade;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateAcademicYear;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateCity;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateCountry;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateCurriculum;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateDistrict;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateEducationalStage;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateFacility;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateGovernorate;
using Schoolera.Application.Admin.Taxonomies.Commands.UpdateGrade;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/taxonomies")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminTaxonomiesController(
    ISender mediator,
    ILogger<AdminTaxonomiesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpPost("countries")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CountryAdminDto>>> CreateCountry(
        [FromBody] CreateCountryCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("countries/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CountryAdminDto>>> UpdateCountry(
        Guid id,
        [FromBody] UpdateCountryBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateCountryCommand(
                id,
                body.Code,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("countries/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CountryAdminDto>>> DeactivateCountry(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateCountryCommand(id), cancellationToken);

    [HttpPost("governorates")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<GovernorateAdminDto>>> CreateGovernorate(
        [FromBody] CreateGovernorateCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("governorates/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<GovernorateAdminDto>>> UpdateGovernorate(
        Guid id,
        [FromBody] UpdateGovernorateBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateGovernorateCommand(
                id,
                body.CountryId,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("governorates/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<GovernorateAdminDto>>> DeactivateGovernorate(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateGovernorateCommand(id), cancellationToken);

    [HttpPost("cities")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CityAdminDto>>> CreateCity(
        [FromBody] CreateCityCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("cities/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CityAdminDto>>> UpdateCity(
        Guid id,
        [FromBody] UpdateCityBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateCityCommand(
                id,
                body.GovernorateId,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("cities/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CityAdminDto>>> DeactivateCity(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateCityCommand(id), cancellationToken);

    [HttpPost("districts")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<DistrictAdminDto>>> CreateDistrict(
        [FromBody] CreateDistrictCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("districts/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<DistrictAdminDto>>> UpdateDistrict(
        Guid id,
        [FromBody] UpdateDistrictBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateDistrictCommand(
                id,
                body.CityId,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("districts/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<DistrictAdminDto>>> DeactivateDistrict(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateDistrictCommand(id), cancellationToken);

    [HttpPost("curricula")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<TaxonomyAdminDto>>> CreateCurriculum(
        [FromBody] CreateCurriculumCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("curricula/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<TaxonomyAdminDto>>> UpdateCurriculum(
        Guid id,
        [FromBody] UpdateCurriculumBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateCurriculumCommand(
                id,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("curricula/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<TaxonomyAdminDto>>> DeactivateCurriculum(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateCurriculumCommand(id), cancellationToken);

    [HttpPost("educational-stages")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<TaxonomyAdminDto>>> CreateEducationalStage(
        [FromBody] CreateEducationalStageCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("educational-stages/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<TaxonomyAdminDto>>> UpdateEducationalStage(
        Guid id,
        [FromBody] UpdateEducationalStageBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateEducationalStageCommand(
                id,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("educational-stages/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<TaxonomyAdminDto>>> DeactivateEducationalStage(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateEducationalStageCommand(id), cancellationToken);

    [HttpPost("grades")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<GradeAdminDto>>> CreateGrade(
        [FromBody] CreateGradeCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("grades/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<GradeAdminDto>>> UpdateGrade(
        Guid id,
        [FromBody] UpdateGradeBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateGradeCommand(
                id,
                body.EducationalStageId,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("grades/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<GradeAdminDto>>> DeactivateGrade(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateGradeCommand(id), cancellationToken);

    [HttpPost("facilities")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FacilityAdminDto>>> CreateFacility(
        [FromBody] CreateFacilityCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("facilities/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FacilityAdminDto>>> UpdateFacility(
        Guid id,
        [FromBody] UpdateFacilityBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateFacilityCommand(
                id,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.IconKey,
                body.SortOrder,
                body.IsActive),
            cancellationToken);

    [HttpPost("facilities/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FacilityAdminDto>>> DeactivateFacility(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateFacilityCommand(id), cancellationToken);

    [HttpPost("academic-years")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<AcademicYearAdminDto>>> CreateAcademicYear(
        [FromBody] CreateAcademicYearCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("academic-years/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<AcademicYearAdminDto>>> UpdateAcademicYear(
        Guid id,
        [FromBody] UpdateAcademicYearBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateAcademicYearCommand(
                id,
                body.NameAr,
                body.NameEn,
                body.Slug,
                body.StartDate,
                body.EndDate,
                body.IsCurrent,
                body.IsActive),
            cancellationToken);

    [HttpPost("academic-years/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<AcademicYearAdminDto>>> DeactivateAcademicYear(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateAcademicYearCommand(id), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));

    public sealed record UpdateCountryBody(
        string Code,
        string NameAr,
        string? NameEn,
        string Slug,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateGovernorateBody(
        Guid CountryId,
        string NameAr,
        string? NameEn,
        string Slug,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateCityBody(
        Guid? GovernorateId,
        string NameAr,
        string? NameEn,
        string Slug,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateDistrictBody(
        Guid CityId,
        string NameAr,
        string? NameEn,
        string Slug,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateCurriculumBody(
        string NameAr,
        string? NameEn,
        string Slug,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateEducationalStageBody(
        string NameAr,
        string? NameEn,
        string Slug,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateGradeBody(
        Guid EducationalStageId,
        string NameAr,
        string? NameEn,
        string Slug,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateFacilityBody(
        string NameAr,
        string? NameEn,
        string Slug,
        string? IconKey,
        int SortOrder,
        bool IsActive);

    public sealed record UpdateAcademicYearBody(
        string NameAr,
        string? NameEn,
        string Slug,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsCurrent,
        bool IsActive);
}
