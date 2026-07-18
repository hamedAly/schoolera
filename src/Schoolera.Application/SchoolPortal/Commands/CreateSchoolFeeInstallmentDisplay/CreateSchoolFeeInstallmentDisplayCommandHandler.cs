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
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolFeeInstallmentDisplay;

public sealed record CreateSchoolFeeInstallmentDisplayCommand(
    Guid SchoolId,
    Guid FeeId,
    CreateSchoolFeeInstallmentDisplayRequest Body) : IRequest<Result<SchoolFeeInstallmentDisplayDto>>;

public sealed class CreateSchoolFeeInstallmentDisplayCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolFeeInstallmentDisplayCommandHandler> logger)
    : IRequestHandler<CreateSchoolFeeInstallmentDisplayCommand, Result<SchoolFeeInstallmentDisplayDto>>
{
    public async Task<Result<SchoolFeeInstallmentDisplayDto>> Handle(
        CreateSchoolFeeInstallmentDisplayCommand request,
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

        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body.NameAr) || !Enum.IsDefined(body.AmountMode))
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeInstallmentDisplayDto>(
                localizer, SchoolPortalErrorCodes.InvalidInstallment);
        }

        SchoolFeeInstallmentDisplay installment;
        try
        {
            installment = new SchoolFeeInstallmentDisplay(
                fee.Id,
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

        await repository.AddInstallmentAsync(installment, cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolFeeInstallmentDisplayDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Created installment {InstallmentId} for fee {FeeId}.",
            installment.Id,
            fee.Id);
        return Result<SchoolFeeInstallmentDisplayDto>.Success(
            SchoolPortalReadModel.ToInstallment(installment));
    }
}
