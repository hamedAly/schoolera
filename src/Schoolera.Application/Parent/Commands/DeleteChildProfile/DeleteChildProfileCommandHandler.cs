using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Parent.Commands.DeleteChildProfile;

public sealed record DeleteChildProfileCommand(Guid ChildId) : IRequest<Result<bool>>;

public sealed class DeleteChildProfileCommandHandler(
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    IAdmissionApplicationRepository admissionRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<DeleteChildProfileCommandHandler> logger)
    : IRequestHandler<DeleteChildProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteChildProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<bool>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var child = await childProfileRepository.GetOwnedForUpdateAsync(userId, request.ChildId, cancellationToken);
        if (child is null || !child.IsActive)
        {
            return Result<bool>.Failure(
                ["Child not found."],
                [ParentErrorCodes.ChildNotFound]);
        }

        if (await admissionRepository.HasAnyForChildAsync(child.Id, cancellationToken))
        {
            return Result<bool>.Failure(
                ["Child cannot be deleted because admission applications reference it."],
                [ParentErrorCodes.CannotDeleteReferencedChild]);
        }

        child.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deactivated child {ChildId} for parent {UserId}.", child.Id, userId);

        return Result<bool>.Success(true);
    }
}
