using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Queries.ListAdminAdmissionApplications;

public sealed record ListAdminAdmissionApplicationsQuery(
    AdminAdmissionApplicationListQuery Filters)
    : IRequest<Result<PagedResult<AdminAdmissionApplicationListItemDto>>>
{
    public static ListAdminAdmissionApplicationsQuery FromFilters(
        string? search,
        Guid? schoolId,
        Guid? cityId,
        int? status,
        Guid? branchId,
        Guid? gradeId,
        Guid? academicYearId,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        string? sort,
        int pageNumber,
        int pageSize) =>
        new(
            new AdminAdmissionApplicationListQuery(
                search,
                schoolId,
                cityId,
                status is null ? null : (AdmissionApplicationStatus)status.Value,
                branchId,
                gradeId,
                academicYearId,
                dateFrom,
                dateTo,
                string.IsNullOrWhiteSpace(sort) ? "newest" : sort.Trim(),
                pageNumber,
                pageSize));
}

public sealed class ListAdminAdmissionApplicationsQueryHandler(
    IAdmissionApplicationRepository admissionRepository,
    ILogger<ListAdminAdmissionApplicationsQueryHandler> logger)
    : IRequestHandler<ListAdminAdmissionApplicationsQuery, Result<PagedResult<AdminAdmissionApplicationListItemDto>>>
{
    public async Task<Result<PagedResult<AdminAdmissionApplicationListItemDto>>> Handle(
        ListAdminAdmissionApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await admissionRepository.ListForAdminAsync(
            request.Filters,
            AdmissionResults.PreferredLanguageCode(),
            cancellationToken);

        logger.LogInformation(
            "Listed {Count} admin admission applications (page {PageNumber}).",
            page.Items.Count,
            page.PageNumber);

        return Result<PagedResult<AdminAdmissionApplicationListItemDto>>.Success(page);
    }
}
