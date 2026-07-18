using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Queries.ListInterviewFaqItems;

public sealed record ListInterviewFaqItemsQuery(
    InterviewFaqCategory? Category,
    bool? IsPublished,
    bool? IsActive,
    string? Search) : IRequest<Result<IReadOnlyList<FaqItemAdminDto>>>
{
    public static ListInterviewFaqItemsQuery FromFilters(
        int? category, bool? isPublished, bool? isActive, string? search) =>
        new(category is { } value ? (InterviewFaqCategory?)value : null,
            isPublished, isActive, search);
}

public sealed class ListInterviewFaqItemsQueryHandler(
    ICmsRepository cmsRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListInterviewFaqItemsQuery, Result<IReadOnlyList<FaqItemAdminDto>>>
{
    public async Task<Result<IReadOnlyList<FaqItemAdminDto>>> Handle(
        ListInterviewFaqItemsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<IReadOnlyList<FaqItemAdminDto>>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var items = await cmsRepository.ListInterviewFaqsAsync(
            schoolId: null,
            interviewCategory: request.Category,
            publishedOnly: false,
            activeOnly: false,
            isPublished: request.IsPublished,
            isActive: request.IsActive,
            search: request.Search,
            ownershipScope: FaqOwnershipScope.Platform,
            cancellationToken: cancellationToken);

        return Result<IReadOnlyList<FaqItemAdminDto>>.Success(
            items.Select(FaqItemAdminDto.FromEntity).ToArray());
    }
}
