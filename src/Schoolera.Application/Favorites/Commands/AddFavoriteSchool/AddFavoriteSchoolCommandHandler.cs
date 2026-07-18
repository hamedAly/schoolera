using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Favorites.Constants;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Favorites.Commands.AddFavoriteSchool;

public sealed record AddFavoriteSchoolCommand(Guid SchoolId) : IRequest<Result<bool>>;

public sealed class AddFavoriteSchoolCommandHandler(
    ICurrentUser currentUser,
    IFavoriteSchoolRepository favoriteSchoolRepository,
    IUnitOfWork unitOfWork,
    ILogger<AddFavoriteSchoolCommandHandler> logger)
    : IRequestHandler<AddFavoriteSchoolCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        AddFavoriteSchoolCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<bool>.Failure(["Forbidden."], [FavoriteErrorCodes.Forbidden]);
        }

        if (!await favoriteSchoolRepository.SchoolExistsAsync(request.SchoolId, cancellationToken))
        {
            return Result<bool>.Failure(["School not found."], [FavoriteErrorCodes.SchoolNotFound]);
        }

        if (await favoriteSchoolRepository.ExistsAsync(userId, request.SchoolId, cancellationToken))
        {
            logger.LogInformation(
                "Favorite already exists for parent {ParentUserId} school {SchoolId}.",
                userId,
                request.SchoolId);
            return Result<bool>.Success(true);
        }

        await favoriteSchoolRepository.AddAsync(new FavoriteSchool(userId, request.SchoolId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Parent {ParentUserId} favorited school {SchoolId}.",
            userId,
            request.SchoolId);
        return Result<bool>.Success(true);
    }
}
