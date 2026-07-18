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

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolBranch;

public sealed record UpdateSchoolBranchCommand(
    Guid SchoolId,
    Guid BranchId,
    UpdateSchoolBranchRequest Body) : IRequest<Result<SchoolBranchDto>>;

public sealed class UpdateSchoolBranchCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolBranchCommandHandler> logger)
    : IRequestHandler<UpdateSchoolBranchCommand, Result<SchoolBranchDto>>
{
    public async Task<Result<SchoolBranchDto>> Handle(
        UpdateSchoolBranchCommand request,
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

        var body = request.Body;
        if (!await repository.DistrictBelongsToCityAsync(body.DistrictId, body.CityId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolBranchDto>(
                localizer, SchoolPortalErrorCodes.CityDistrictMismatch);
        }

        branch.UpdateIdentity(body.NameAr, body.NameEn, body.CityId, body.DistrictId);
        branch.UpdateAddress(
            body.AddressLineAr,
            body.AddressLineEn,
            body.BuildingNumber,
            body.StreetName,
            body.Landmark,
            body.PostalCode,
            body.AddressReference,
            body.Latitude,
            body.Longitude);
        branch.UpdateContact(body.Phone, body.Email);

        if (body.IsMainBranch && !branch.IsMainBranch)
        {
            await ClearOtherMainBranchesAsync(request.SchoolId, branch.Id, cancellationToken);
            branch.SetMainBranch(true);
        }
        else if (!body.IsMainBranch && branch.IsMainBranch)
        {
            if (!await repository.HasOtherActiveMainBranchAsync(request.SchoolId, branch.Id, cancellationToken))
            {
                return SchoolPortalResults.FailureForCode<SchoolBranchDto>(
                    localizer, SchoolPortalErrorCodes.MainBranchRequired);
            }

            branch.SetMainBranch(false);
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolBranchDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolBranchDto>.Success(SchoolPortalReadModel.ToBranch(branch));
    }

    private async Task ClearOtherMainBranchesAsync(
        Guid schoolId,
        Guid excludeBranchId,
        CancellationToken cancellationToken)
    {
        var branches = await repository.ListBranchesAsync(schoolId, cancellationToken);
        foreach (var existing in branches.Where(entry => entry.IsMainBranch && entry.Id != excludeBranchId))
        {
            var tracked = await repository.GetBranchForWriteAsync(schoolId, existing.Id, cancellationToken);
            tracked?.SetMainBranch(false);
        }
    }
}
