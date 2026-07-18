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
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolBranch;

public sealed record CreateSchoolBranchCommand(
    Guid SchoolId,
    CreateSchoolBranchRequest Body) : IRequest<Result<SchoolBranchDto>>;

public sealed class CreateSchoolBranchCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolBranchCommandHandler> logger)
    : IRequestHandler<CreateSchoolBranchCommand, Result<SchoolBranchDto>>
{
    public async Task<Result<SchoolBranchDto>> Handle(
        CreateSchoolBranchCommand request,
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

        var body = request.Body;
        if (!await repository.DistrictBelongsToCityAsync(body.DistrictId, body.CityId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolBranchDto>(
                localizer, SchoolPortalErrorCodes.CityDistrictMismatch);
        }

        var slug = await SchoolPortalBranchSlug.CreateUniqueAsync(
            repository, request.SchoolId, body.NameAr, body.NameEn, null, cancellationToken);

        var branch = new SchoolBranch(
            request.SchoolId,
            body.NameAr,
            body.NameEn,
            slug,
            body.CityId,
            body.DistrictId,
            body.IsMainBranch);

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

        if (body.IsMainBranch)
        {
            await ClearOtherMainBranchesAsync(request.SchoolId, null, cancellationToken);
        }

        var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);
        school?.AddBranch(branch);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolBranchDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await repository.GetBranchForWriteAsync(request.SchoolId, branch.Id, cancellationToken);
        return Result<SchoolBranchDto>.Success(SchoolPortalReadModel.ToBranch(loaded!));
    }

    private async Task ClearOtherMainBranchesAsync(
        Guid schoolId,
        Guid? excludeBranchId,
        CancellationToken cancellationToken)
    {
        var branches = await repository.ListBranchesAsync(schoolId, cancellationToken);
        foreach (var existing in branches.Where(branch => branch.IsMainBranch &&
                                                          (excludeBranchId == null || branch.Id != excludeBranchId)))
        {
            var tracked = await repository.GetBranchForWriteAsync(schoolId, existing.Id, cancellationToken);
            tracked?.SetMainBranch(false);
        }
    }
}
