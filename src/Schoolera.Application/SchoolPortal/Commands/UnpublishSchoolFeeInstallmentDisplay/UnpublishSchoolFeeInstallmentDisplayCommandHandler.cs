using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolFeeInstallmentDisplay;

public sealed record UnpublishSchoolFeeInstallmentDisplayCommand(
    Guid SchoolId,
    Guid FeeId,
    Guid ItemId) : IRequest<Result<SchoolFeeInstallmentDisplayDto>>;

public sealed class UnpublishSchoolFeeInstallmentDisplayCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UnpublishSchoolFeeInstallmentDisplayCommandHandler> logger)
    : IRequestHandler<UnpublishSchoolFeeInstallmentDisplayCommand, Result<SchoolFeeInstallmentDisplayDto>>
{
    public async Task<Result<SchoolFeeInstallmentDisplayDto>> Handle(
        UnpublishSchoolFeeInstallmentDisplayCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolFeeInstallmentDisplayDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolFeeInstallmentDisplayDto>(
            accessResult.Data, SchoolPortalPermission.ManageFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var fee = await repository.GetTuitionFeeForWriteAsync(
            request.SchoolId, request.FeeId, cancellationToken);
        if (fee is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeInstallmentDisplayDto>(
                localizer, SchoolPortalErrorCodes.FeeNotFound);
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<SchoolFeeInstallmentDisplayDto>(
            accessResult.Data, fee.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        var entity = await repository.GetInstallmentForWriteAsync(request.SchoolId, request.FeeId, request.ItemId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeInstallmentDisplayDto>(
                localizer, SchoolPortalErrorCodes.InstallmentNotFound);
        }

        entity.Unpublish();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolFeeInstallmentDisplayDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolFeeInstallmentDisplayDto>.Success(SchoolPortalReadModel.ToInstallment(entity));
    }
}
