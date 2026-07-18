using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Queries.ListAdmissionApplications;

public sealed record ListAdmissionApplicationsQuery(
    AdmissionApplicationStatus? Status = null,
    Guid? ChildProfileId = null,
    Guid? SchoolId = null,
    Guid? AcademicYearId = null,
    string? Search = null,
    string? Sort = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<AdmissionApplicationListItemDto>>>
{
    public static ListAdmissionApplicationsQuery FromFilters(
        int? status,
        Guid? childProfileId,
        Guid? schoolId,
        Guid? academicYearId,
        string? search,
        string? sort,
        int pageNumber,
        int pageSize) =>
        new(
            status is null ? null : (AdmissionApplicationStatus)status.Value,
            childProfileId,
            schoolId,
            academicYearId,
            search,
            sort,
            pageNumber,
            pageSize);
}

public sealed class ListAdmissionApplicationsQueryValidator
    : AbstractValidator<ListAdmissionApplicationsQuery>
{
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "newest",
        "oldest",
        "application-number",
        "status",
    };

    public ListAdmissionApplicationsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status is not null);
        RuleFor(query => query.Search).MaximumLength(200).When(query => !string.IsNullOrWhiteSpace(query.Search));
        RuleFor(query => query.Sort)
            .Must(sort => string.IsNullOrWhiteSpace(sort) || AllowedSorts.Contains(sort.Trim()))
            .WithMessage("Sort must be one of: newest, oldest, application-number, status.");
    }
}

public sealed class ListAdmissionApplicationsQueryHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<ListAdmissionApplicationsQueryHandler> logger)
    : IRequestHandler<ListAdmissionApplicationsQuery, Result<PagedResult<AdmissionApplicationListItemDto>>>
{
    public async Task<Result<PagedResult<AdmissionApplicationListItemDto>>> Handle(
        ListAdmissionApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<PagedResult<AdmissionApplicationListItemDto>>(localizer);
        }

        var query = new AdmissionApplicationListQuery(
            request.Status,
            request.ChildProfileId,
            request.SchoolId,
            request.AcademicYearId,
            request.Search,
            string.IsNullOrWhiteSpace(request.Sort) ? "newest" : request.Sort.Trim(),
            request.PageNumber,
            request.PageSize);

        var page = await admissionRepository.ListForParentAsync(
            userId,
            query,
            AdmissionResults.PreferredLanguageCode(),
            cancellationToken);

        logger.LogInformation(
            "Listed {Count} admission applications for parent {UserId} (page {PageNumber}).",
            page.Items.Count,
            userId,
            page.PageNumber);

        return Result<PagedResult<AdmissionApplicationListItemDto>>.Success(page);
    }
}
