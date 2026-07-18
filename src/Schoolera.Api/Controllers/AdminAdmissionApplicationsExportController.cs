using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Queries.ExportAdminAdmissionApplications;
using Schoolera.Application.Auth.Constants;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Streams admin admission CSV exports. Kept separate from
/// <see cref="AdminAdmissionApplicationsController"/> because file downloads return a file result
/// rather than the standard <c>Result&lt;T&gt;</c> envelope.
/// </summary>
[Route("api/admin/admission-applications")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminAdmissionApplicationsExportController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] Guid? schoolId,
        [FromQuery] Guid? cityId,
        [FromQuery] int? status,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] string? sort,
        [FromQuery] bool includeAnswers = false,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            ExportAdminAdmissionApplicationsQuery.FromFilters(
                search, schoolId, cityId, status, branchId, gradeId, academicYearId,
                dateFrom, dateTo, sort, includeAnswers),
            cancellationToken);
        return result.ToAdminCsvDownloadResult(this);
    }
}
