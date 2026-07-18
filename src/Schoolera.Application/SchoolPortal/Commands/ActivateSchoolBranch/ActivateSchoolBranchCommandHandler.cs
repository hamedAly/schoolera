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

namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolBranch;

public sealed record ActivateSchoolBranchCommand(Guid SchoolId, Guid BranchId) : IRequest<Result<SchoolBranchDto>>;

public sealed class ActivateSchoolBranchCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ActivateSchoolBranchCommandHandler> logger)
    : IRequestHandler<ActivateSchoolBranchCommand, Result<SchoolBranchDto>>
{
    public async Task<Result<SchoolBranchDto>> Handle(
        ActivateSchoolBranchCommand request,
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

        branch.Activate();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolBranchDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolBranchDto>.Success(SchoolPortalReadModel.ToBranch(branch));
    }
}
