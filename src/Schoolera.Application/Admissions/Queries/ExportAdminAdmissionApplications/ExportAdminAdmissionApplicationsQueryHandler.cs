using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Queries.ExportAdminAdmissionApplications;

public sealed record AdminAdmissionExportFileDto(byte[] Content, string FileName);

public sealed record ExportAdminAdmissionApplicationsQuery(
    AdminAdmissionApplicationListQuery Filters,
    bool IncludeAnswers = false)
    : IRequest<Result<AdminAdmissionExportFileDto>>
{
    public static ExportAdminAdmissionApplicationsQuery FromFilters(
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
        bool includeAnswers = false) =>
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
                PageNumber: 1,
                PageSize: AdminAdmissionCsvExporter.MaxExportRows),
            includeAnswers);
}

public sealed class ExportAdminAdmissionApplicationsQueryHandler(
    IAdmissionApplicationRepository admissionRepository,
    ILogger<ExportAdminAdmissionApplicationsQueryHandler> logger)
    : IRequestHandler<ExportAdminAdmissionApplicationsQuery, Result<AdminAdmissionExportFileDto>>
{
    public async Task<Result<AdminAdmissionExportFileDto>> Handle(
        ExportAdminAdmissionApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var answerColumns = request.IncludeAnswers
            ? await admissionRepository.ResolveExportAnswerColumnsAsync(
                request.Filters,
                AdminAdmissionCsvExporter.MaxAnswerColumns,
                cancellationToken)
            : Array.Empty<string>();

        var rows = await admissionRepository.ListForAdminExportAsync(
            request.Filters,
            AdmissionResults.PreferredLanguageCode(),
            AdminAdmissionCsvExporter.MaxExportRows,
            request.IncludeAnswers,
            answerColumns,
            cancellationToken);

        var (content, fileName) = AdminAdmissionCsvExporter.Build(
            rows,
            request.IncludeAnswers,
            answerColumns);

        logger.LogInformation(
            "Exported {Count} admin admission applications to {FileName}.",
            rows.Count,
            fileName);

        return Result<AdminAdmissionExportFileDto>.Success(
            new AdminAdmissionExportFileDto(content, fileName));
    }
}
