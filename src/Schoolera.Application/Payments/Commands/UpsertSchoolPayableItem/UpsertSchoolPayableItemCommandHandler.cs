using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Payments.Commands.UpsertSchoolPayableItem;

public sealed record UpsertSchoolPayableItemCommand(Guid SchoolId, UpsertSchoolPayableItemRequest Body)
    : IRequest<Result<SchoolPayableItemAdminDto>>;

public sealed class UpsertSchoolPayableItemCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository schoolPortalRepository,
    IPaymentRepository paymentRepository,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpsertSchoolPayableItemCommandHandler> logger)
    : IRequestHandler<UpsertSchoolPayableItemCommand, Result<SchoolPayableItemAdminDto>>
{
    public async Task<Result<SchoolPayableItemAdminDto>> Handle(
        UpsertSchoolPayableItemCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolPayableItemAdminDto>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolPayableItemAdminDto>(
            accessResult.Data, SchoolPortalPermission.ManageFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var body = request.Body;
        if (!PaymentMapping.TryParseRowVersion(body.RowVersion, out var rowVersion))
        {
            return Result<SchoolPayableItemAdminDto>.Failure(
                ["Invalid row version."],
                [PaymentErrorCodes.ValidationFailed]);
        }

        var fee = await schoolPortalRepository.GetTuitionFeeForWriteAsync(
            request.SchoolId, body.TuitionFeeId, cancellationToken);
        if (fee is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolPayableItemAdminDto>(
                localizer, SchoolPortalErrorCodes.InvalidFee);
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<SchoolPayableItemAdminDto>(
            accessResult.Data, fee.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        var existing = await paymentRepository.GetPayableItemBySchoolAndFeeAsync(
            request.SchoolId, body.TuitionFeeId, cancellationToken);

        SchoolPayableItem item;
        if (existing is null)
        {
            item = new SchoolPayableItem(
                request.SchoolId,
                fee.SchoolBranchId,
                fee.Id,
                body.PayableFromUtc,
                body.PayableToUtc,
                body.PaymentInstructionsAr,
                body.PaymentInstructionsEn);
            if (!body.IsActive)
            {
                item.Deactivate();
            }

            await paymentRepository.AddPayableItemAsync(item, cancellationToken);
        }
        else
        {
            if (PaymentMapping.HasRowVersionMismatch(rowVersion, existing.RowVersion))
            {
                return Result<SchoolPayableItemAdminDto>.Failure(
                    ["Concurrency conflict."],
                    [PaymentErrorCodes.ConcurrencyConflict]);
            }

            existing.UpdateWindow(
                body.PayableFromUtc,
                body.PayableToUtc,
                body.PaymentInstructionsAr,
                body.PaymentInstructionsEn);
            if (body.IsActive)
            {
                existing.Activate();
            }
            else
            {
                existing.Deactivate();
            }

            item = existing;
        }

        // Reload with tuition fee navigation for mapping when newly created.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var persisted = await paymentRepository.GetPayableItemAsync(item.Id, cancellationToken)
                        ?? item;

        await adminPlatform.WriteAuditAsync(
            accessResult.Data.UserId,
            PaymentAuditActions.PayableItemUpserted,
            "SchoolPayableItem",
            persisted.Id.ToString(),
            $"Upserted payable item for fee {body.TuitionFeeId}.",
            cancellationToken);

        logger.LogInformation(
            "School {SchoolId} upserted payable item {PayableItemId} for fee {TuitionFeeId}.",
            request.SchoolId,
            persisted.Id,
            body.TuitionFeeId);

        return Result<SchoolPayableItemAdminDto>.Success(
            PaymentMapping.ToSchoolPayableAdminDto(persisted));
    }
}
