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

namespace Schoolera.Application.Admin.Commands.UpdateSchoolStatus;

public sealed record UpdateAdminSchoolStatusCommand(Guid SchoolId, string Status)
    : IRequest<Result<AdminSchoolDetailDto>>;

public sealed class UpdateAdminSchoolStatusCommandHandler(
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UpdateAdminSchoolStatusCommandHandler> logger)
    : IRequestHandler<UpdateAdminSchoolStatusCommand, Result<AdminSchoolDetailDto>>
{
    public async Task<Result<AdminSchoolDetailDto>> Handle(
        UpdateAdminSchoolStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<AdminSchoolDetailDto>.Failure(
                [localizer["Forbidden"].Value],
                [AdminErrorCodes.Forbidden]);
        }

        if (!Enum.TryParse<SchoolStatus>(request.Status, ignoreCase: true, out var status))
        {
            return Result<AdminSchoolDetailDto>.Failure(
                [localizer["AdminInvalidSchoolStatus"].Value],
                [AdminErrorCodes.InvalidSchoolStatus]);
        }

        var result = await adminPlatform.UpdateSchoolStatusAsync(
            request.SchoolId,
            status,
            actorId,
            cancellationToken);

        // Persistence is committed inside AdminPlatformService; IUnitOfWork is required by
        // project architecture conventions for non-auth commands.
        _ = unitOfWork;

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Platform admin {ActorUserId} set school {SchoolId} status to {Status}.",
                actorId,
                request.SchoolId,
                status);
        }

        return result;
    }
}
