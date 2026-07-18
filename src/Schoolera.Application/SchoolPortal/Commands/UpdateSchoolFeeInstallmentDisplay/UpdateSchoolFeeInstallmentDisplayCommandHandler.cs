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

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFeeInstallmentDisplay;

public sealed record UpdateSchoolFeeInstallmentDisplayCommand(
    Guid SchoolId,
    Guid FeeId,
    Guid InstallmentId,
    UpdateSchoolFeeInstallmentDisplayRequest Body) : IRequest<Result<SchoolFeeInstallmentDisplayDto>>;

public sealed class UpdateSchoolFeeInstallmentDisplayCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolFeeInstallmentDisplayCommandHandler> logger)
    : IRequestHandler<UpdateSchoolFeeInstallmentDisplayCommand, Result<SchoolFeeInstallmentDisplayDto>>
{
    public async Task<Result<SchoolFeeInstallmentDisplayDto>> Handle(
        UpdateSchoolFeeInstallmentDisplayCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolFeeInstallmentDisplayDto>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
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

        var installment = await repository.GetInstallmentForWriteAsync(
            request.SchoolId, request.FeeId, request.InstallmentId, cancellationToken);
        if (installment is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeInstallmentDisplayDto>(
                localizer, SchoolPortalErrorCodes.InstallmentNotFound);
        }

        var body = request.Body;
        try
        {
            installment.Update(
                body.SequenceNumber,
                body.NameAr,
                body.NameEn,
                body.AmountMode,
                body.FixedAmount,
                body.Percentage,
                body.DueDateUtc,
                body.DueWindowStartUtc,
                body.DueWindowEndUtc,
                body.NotesAr,
                body.NotesEn,
                body.SortOrder);
        }
        catch (ArgumentException)
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeInstallmentDisplayDto>(
                localizer, SchoolPortalErrorCodes.InvalidInstallment);
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolFeeInstallmentDisplayDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolFeeInstallmentDisplayDto>.Success(
            SchoolPortalReadModel.ToInstallment(installment));
    }
}
