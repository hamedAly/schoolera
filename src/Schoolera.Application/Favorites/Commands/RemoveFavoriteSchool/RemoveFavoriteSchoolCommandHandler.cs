using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Favorites.Constants;

namespace Schoolera.Application.Favorites.Commands.RemoveFavoriteSchool;

public sealed record RemoveFavoriteSchoolCommand(Guid SchoolId) : IRequest<Result<bool>>;

public sealed class RemoveFavoriteSchoolCommandHandler(
    ICurrentUser currentUser,
    IFavoriteSchoolRepository favoriteSchoolRepository,
    IUnitOfWork unitOfWork,
    ILogger<RemoveFavoriteSchoolCommandHandler> logger)
    : IRequestHandler<RemoveFavoriteSchoolCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveFavoriteSchoolCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<bool>.Failure(["Forbidden."], [FavoriteErrorCodes.Forbidden]);
        }

        var favorite = await favoriteSchoolRepository.GetAsync(userId, request.SchoolId, cancellationToken);
        if (favorite is null)
        {
            logger.LogInformation(
                "Favorite already absent for parent {ParentUserId} school {SchoolId}.",
                userId,
                request.SchoolId);
            return Result<bool>.Success(true);
        }

        await favoriteSchoolRepository.RemoveAsync(favorite, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Parent {ParentUserId} removed favorite school {SchoolId}.",
            userId,
            request.SchoolId);
        return Result<bool>.Success(true);
    }
}
