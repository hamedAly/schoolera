using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Queries.ListCmsPages;

public sealed record ListCmsPagesQuery(
    string? Search,
    string? Status,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<CmsPageListItemDto>>>;

public sealed class ListCmsPagesQueryHandler(
    ICmsRepository cmsRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListCmsPagesQuery, Result<PagedResult<CmsPageListItemDto>>>
{
    public async Task<Result<PagedResult<CmsPageListItemDto>>> Handle(
        ListCmsPagesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<PagedResult<CmsPageListItemDto>>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        CmsPublicationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<CmsPublicationStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        var pages = await cmsRepository.ListPagesAsync(request.Search, status, paging, cancellationToken);
        var items = pages.Items
            .Select(page => new CmsPageListItemDto(
                page.Id,
                page.Slug,
                page.TitleAr,
                page.TitleEn,
                page.Status,
                page.IsSystemPage,
                page.PublishedAtUtc,
                page.UpdatedAtUtc))
            .ToArray();

        return Result<PagedResult<CmsPageListItemDto>>.Success(
            PagedResult<CmsPageListItemDto>.Create(items, pages.TotalCount, paging));
    }
}
