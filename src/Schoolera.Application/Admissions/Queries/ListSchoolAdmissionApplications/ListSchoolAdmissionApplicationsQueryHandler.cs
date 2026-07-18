using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Queries.ListSchoolAdmissionApplications;

public sealed record ListSchoolAdmissionApplicationsQuery(
    Guid SchoolId,
    SchoolAdmissionApplicationListQuery Filters)
    : IRequest<Result<PagedResult<SchoolAdmissionApplicationListItemDto>>>
{
    public static ListSchoolAdmissionApplicationsQuery FromFilters(
        Guid schoolId,
        int? status,
        Guid? branchId,
        Guid? gradeId,
        Guid? educationalStageId,
        Guid? academicYearId,
        string? search,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        string? sort,
        int pageNumber,
        int pageSize) =>
        new(
            schoolId,
            new SchoolAdmissionApplicationListQuery(
                status is null ? null : (AdmissionApplicationStatus)status.Value,
                branchId,
                gradeId,
                educationalStageId,
                academicYearId,
                search,
                dateFrom,
                dateTo,
                string.IsNullOrWhiteSpace(sort) ? "newest" : sort.Trim(),
                pageNumber,
                pageSize));
}

public sealed class ListSchoolAdmissionApplicationsQueryHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ListSchoolAdmissionApplicationsQueryHandler> logger)
    : IRequestHandler<ListSchoolAdmissionApplicationsQuery, Result<PagedResult<SchoolAdmissionApplicationListItemDto>>>
{
    public async Task<Result<PagedResult<SchoolAdmissionApplicationListItemDto>>> Handle(
        ListSchoolAdmissionApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<PagedResult<SchoolAdmissionApplicationListItemDto>>.Failure(
                accessResult.Errors,
                accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        var permissionCheck = SchoolPortalAccess.RequirePermission<PagedResult<SchoolAdmissionApplicationListItemDto>>(
            access, SchoolPortalPermission.ViewApplications, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var filters = request.Filters;
        if (!access.AllowsAllBranches)
        {
            if (filters.BranchId is { } branchId)
            {
                var branchCheck = SchoolPortalAccess.RequireBranch<PagedResult<SchoolAdmissionApplicationListItemDto>>(
                    access, branchId, localizer);
                if (!branchCheck.Succeeded)
                {
                    return branchCheck;
                }

                filters = filters with { RestrictToBranchIds = [branchId] };
            }
            else
            {
                filters = filters with
                {
                    BranchId = null,
                    RestrictToBranchIds = access.AllowedBranchIds.ToArray(),
                };
            }
        }
        else if (filters.BranchId is { } requestedBranch)
        {
            var branchCheck = SchoolPortalAccess.RequireBranch<PagedResult<SchoolAdmissionApplicationListItemDto>>(
                access, requestedBranch, localizer);
            if (!branchCheck.Succeeded)
            {
                return branchCheck;
            }
        }

        var page = await admissionRepository.ListForSchoolFilteredAsync(
            request.SchoolId,
            filters,
            AdmissionResults.PreferredLanguageCode(),
            cancellationToken);

        logger.LogInformation(
            "Listed {Count} school admission applications for school {SchoolId} (page {PageNumber}).",
            page.Items.Count,
            request.SchoolId,
            page.PageNumber);

        return Result<PagedResult<SchoolAdmissionApplicationListItemDto>>.Success(page);
    }
}
