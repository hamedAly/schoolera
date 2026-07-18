using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolInterviewFaqs;

public sealed record ListSchoolInterviewFaqsQuery(
    Guid SchoolId,
    InterviewFaqCategory? InterviewCategory,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    bool? IsPublished,
    bool? IsActive) : IRequest<Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>>
{
    public static ListSchoolInterviewFaqsQuery FromFilters(
        Guid schoolId,
        int? interviewCategory,
        Guid? branchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        bool? isPublished,
        bool? isActive) =>
        new(schoolId,
            interviewCategory is { } value ? (InterviewFaqCategory?)value : null,
            branchId, educationalStageId, gradeId, academicYearId, isPublished, isActive);
}

public sealed class ListSchoolInterviewFaqsQueryHandler(
    ISchoolPortalAccess portalAccess,
    ICmsRepository cmsRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolInterviewFaqsQuery, Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>> Handle(
        ListSchoolInterviewFaqsQuery request,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await SchoolInterviewFaqSupport.ResolveReadAccessAsync(
            portalAccess, request.SchoolId, localizer, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var items = await cmsRepository.ListSchoolInterviewFaqsAsync(
            request.SchoolId,
            request.InterviewCategory,
            request.IsPublished,
            request.IsActive,
            request.BranchId,
            request.EducationalStageId,
            request.GradeId,
            request.AcademicYearId,
            search: null,
            cancellationToken);

        return Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>.Success(
            items.Select(SchoolInterviewFaqSupport.ToDetail).ToArray());
    }
}
