using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Mapping;

namespace Schoolera.Application.Schools.Queries.GetPublicSchoolBySlug;

public sealed record GetPublicSchoolBySlugQuery(string Slug) : IRequest<Result<PublicSchoolProfileDto>>;

public sealed class GetPublicSchoolBySlugQueryHandler(
    ISchoolReadRepository schoolReadRepository,
    ISchoolProfileViewQueue profileViewQueue,
    IFavoriteSchoolRepository favoriteSchoolRepository,
    ICurrentUser currentUser,
    ILogger<GetPublicSchoolBySlugQueryHandler> logger)
    : IRequestHandler<GetPublicSchoolBySlugQuery, Result<PublicSchoolProfileDto>>
{
    public async Task<Result<PublicSchoolProfileDto>> Handle(
        GetPublicSchoolBySlugQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting published public school by slug {Slug}.", request.Slug);

        var school = await schoolReadRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);
        if (school is null)
        {
            return Result<PublicSchoolProfileDto>.Failure(
                ["School not found."],
                [SchoolErrorCodes.NotFound]);
        }

        // Best-effort analytics: never fail the profile response when the queue is full or unavailable.
        _ = profileViewQueue.TryEnqueue(school.Id, DateTimeOffset.UtcNow);

        var isAuthenticatedParent = currentUser.IsAuthenticated &&
                                    currentUser.IsInRole(SchooleraRoles.Parent);

        bool? isFavorite = null;
        if (isAuthenticatedParent && currentUser.UserId is { } parentUserId)
        {
            isFavorite = await favoriteSchoolRepository.ExistsAsync(
                parentUserId,
                school.Id,
                cancellationToken);
        }

        return Result<PublicSchoolProfileDto>.Success(
            PublicSchoolMapping.ToProfile(school, isAuthenticatedParent, isFavorite));
    }
}
