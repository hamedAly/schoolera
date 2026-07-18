using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Schools.Queries.GetPublicInterviewFaqs;

public sealed record GetPublicInterviewFaqsQuery(
    string Slug,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    InterviewFaqCategory? Category) : IRequest<Result<IReadOnlyList<PublicInterviewFaqItemDto>>>
{
    public static GetPublicInterviewFaqsQuery FromFilters(
        string slug,
        Guid? branchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int? category) =>
        new(slug, branchId, educationalStageId, gradeId, academicYearId,
            category is { } value ? (InterviewFaqCategory?)value : null);
}

public sealed class GetPublicInterviewFaqsQueryHandler(
    ISchoolReadRepository schoolReadRepository,
    ICmsRepository cmsRepository,
    ILogger<GetPublicInterviewFaqsQueryHandler> logger)
    : IRequestHandler<GetPublicInterviewFaqsQuery, Result<IReadOnlyList<PublicInterviewFaqItemDto>>>
{
    public async Task<Result<IReadOnlyList<PublicInterviewFaqItemDto>>> Handle(
        GetPublicInterviewFaqsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Public interview FAQs for slug {Slug}.", request.Slug);

        var school = await schoolReadRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);
        if (school is null)
        {
            return Result<IReadOnlyList<PublicInterviewFaqItemDto>>.Failure(
                ["School not found."],
                [SchoolErrorCodes.NotFound]);
        }

        var candidates = await cmsRepository.ListInterviewFaqsAsync(
            schoolId: school.Id,
            interviewCategory: request.Category,
            publishedOnly: true,
            activeOnly: true,
            branchId: request.BranchId,
            stageId: request.EducationalStageId,
            gradeId: request.GradeId,
            yearId: request.AcademicYearId,
            cancellationToken: cancellationToken);

        var matched = candidates
            .Where(item =>
                item.InterviewCategory is { } category &&
                InterviewFaqApplicability.MatchesCategoryFilter(category, request.Category) &&
                InterviewFaqApplicability.Matches(
                    item,
                    request.BranchId,
                    request.EducationalStageId,
                    request.GradeId,
                    request.AcademicYearId))
            .OrderBy(item => item.OwnershipScope == FaqOwnershipScope.Platform ? 0 : 1)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Select(PublicInterviewFaqItemDto.FromEntity)
            .ToArray();

        return Result<IReadOnlyList<PublicInterviewFaqItemDto>>.Success(matched);
    }
}
