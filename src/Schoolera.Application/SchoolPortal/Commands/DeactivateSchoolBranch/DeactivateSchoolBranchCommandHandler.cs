using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolBranch;

public sealed record DeactivateSchoolBranchCommand(Guid SchoolId, Guid BranchId) : IRequest<Result<SchoolBranchDto>>;

public sealed class DeactivateSchoolBranchCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeactivateSchoolBranchCommandHandler> logger)
    : IRequestHandler<DeactivateSchoolBranchCommand, Result<SchoolBranchDto>>
{
    public async Task<Result<SchoolBranchDto>> Handle(
        DeactivateSchoolBranchCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolBranchDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolBranchDto>(
            accessResult.Data, SchoolPortalPermission.ManageBranches, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var branch = await repository.GetBranchForWriteAsync(request.SchoolId, request.BranchId, cancellationToken);
        if (branch is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolBranchDto>(
                localizer, SchoolPortalErrorCodes.BranchNotFound);
        }

        if (branch.IsMainBranch &&
            !await repository.HasOtherActiveMainBranchAsync(request.SchoolId, branch.Id, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolBranchDto>(
                localizer, SchoolPortalErrorCodes.MainBranchRequired);
        }

        branch.Deactivate();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolBranchDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolBranchDto>.Success(SchoolPortalReadModel.ToBranch(branch));
    }
}
