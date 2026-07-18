using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admin.Commands.UpdateUserStatus;

public sealed record UpdateAdminUserStatusCommand(Guid UserId, string AccountStatus)
    : IRequest<Result<AdminUserListItemDto>>;

public sealed class UpdateAdminUserStatusCommandHandler(
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UpdateAdminUserStatusCommandHandler> logger)
    : IRequestHandler<UpdateAdminUserStatusCommand, Result<AdminUserListItemDto>>
{
    public async Task<Result<AdminUserListItemDto>> Handle(
        UpdateAdminUserStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<AdminUserListItemDto>.Failure(
                [localizer["Forbidden"].Value],
                [AdminErrorCodes.Forbidden]);
        }

        if (!Enum.TryParse<AccountStatus>(request.AccountStatus, ignoreCase: true, out var status)
            || status is not (AccountStatus.Active or AccountStatus.Suspended))
        {
            return Result<AdminUserListItemDto>.Failure(
                [localizer["AdminInvalidAccountStatus"].Value],
                [AdminErrorCodes.InvalidAccountStatus]);
        }

        var result = await adminPlatform.UpdateUserAccountStatusAsync(
            request.UserId,
            status,
            actorId,
            cancellationToken);

        _ = unitOfWork;

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Platform admin {ActorUserId} set user {UserId} account status to {Status}.",
                actorId,
                request.UserId,
                status);
        }

        return result;
    }
}
