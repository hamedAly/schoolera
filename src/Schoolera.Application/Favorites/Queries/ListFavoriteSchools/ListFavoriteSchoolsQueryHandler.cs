using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Favorites.Constants;
using Schoolera.Application.Favorites.Dtos;
using Schoolera.Application.Schools.Mapping;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Favorites.Queries.ListFavoriteSchools;

public sealed record ListFavoriteSchoolsQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<FavoriteSchoolListItemDto>>>;

public sealed class ListFavoriteSchoolsQueryHandler(
    ICurrentUser currentUser,
    IFavoriteSchoolRepository favoriteSchoolRepository,
    ISchoolReadRepository schoolReadRepository,
    ILogger<ListFavoriteSchoolsQueryHandler> logger)
    : IRequestHandler<ListFavoriteSchoolsQuery, Result<PagedResult<FavoriteSchoolListItemDto>>>
{
    public async Task<Result<PagedResult<FavoriteSchoolListItemDto>>> Handle(
        ListFavoriteSchoolsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<PagedResult<FavoriteSchoolListItemDto>>.Failure(
                ["Forbidden."],
                [FavoriteErrorCodes.Forbidden]);
        }

        var paging = new PagedRequest(request.PageNumber, request.PageSize);
        logger.LogInformation("Listing favorite schools for parent {ParentUserId}.", userId);

        var (rows, totalCount) = await favoriteSchoolRepository.ListAsync(userId, paging, cancellationToken);
        var publishedIds = rows
            .Where(row => row.SchoolStatus == SchoolStatus.Published)
            .Select(row => row.SchoolId)
            .ToArray();

        var cards = publishedIds.Length == 0
            ? Array.Empty<PublicSchoolSearchProjection>()
            : await schoolReadRepository.GetPublishedListCardsByIdsAsync(publishedIds, cancellationToken);

        var cardById = cards.ToDictionary(card => card.Id);

        var items = rows.Select(row =>
        {
            if (row.SchoolStatus == SchoolStatus.Published &&
                cardById.TryGetValue(row.SchoolId, out var projection))
            {
                var card = PublicSchoolMapping.ToListItem(projection) with { IsFavorite = true };
                return new FavoriteSchoolListItemDto(
                    row.FavoriteId,
                    row.SchoolId,
                    IsAvailable: true,
                    School: card,
                    Unavailable: null);
            }

            var displayName = LocalizationDisplayHelper.Pick(row.NameAr, row.NameEn);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Unavailable";
            }

            return new FavoriteSchoolListItemDto(
                row.FavoriteId,
                row.SchoolId,
                IsAvailable: false,
                School: null,
                Unavailable: new FavoriteSchoolUnavailableDto(
                    row.FavoriteId,
                    row.SchoolId,
                    Slug: null,
                    DisplayName: displayName,
                    IsAvailable: false,
                    IsAdmissionOpen: false));
        }).ToArray();

        return Result<PagedResult<FavoriteSchoolListItemDto>>.Success(
            PagedResult<FavoriteSchoolListItemDto>.Create(items, totalCount, paging));
    }
}
