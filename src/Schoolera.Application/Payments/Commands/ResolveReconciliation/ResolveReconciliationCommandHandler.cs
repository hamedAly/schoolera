using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Commands.ResolveReconciliation;

public sealed record ResolveReconciliationCommand(Guid Id, ResolveReconciliationRequest Body)
    : IRequest<Result<ReconciliationRecordDto>>;

public sealed class ResolveReconciliationCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    IAdminPlatformService adminPlatform,
    IUnitOfWork unitOfWork,
    ILogger<ResolveReconciliationCommandHandler> logger)
    : IRequestHandler<ResolveReconciliationCommand, Result<ReconciliationRecordDto>>
{
    public async Task<Result<ReconciliationRecordDto>> Handle(
        ResolveReconciliationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<ReconciliationRecordDto>.Failure(
                ["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        if (string.IsNullOrWhiteSpace(request.Body.ResolutionCode))
        {
            return Result<ReconciliationRecordDto>.Failure(
                ["Invalid resolution."],
                [PaymentErrorCodes.InvalidResolution]);
        }

        var record = await paymentRepository.GetReconciliationAsync(request.Id, cancellationToken);
        if (record is null)
        {
            return Result<ReconciliationRecordDto>.Failure(
                ["Not found."], [PaymentErrorCodes.ReconciliationNotFound]);
        }

        record.Resolve(actorId, request.Body.ResolutionCode.Trim(), request.Body.InternalNote);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            PaymentAuditActions.ReconciliationResolved,
            "PaymentReconciliationRecord",
            record.Id.ToString(),
            $"Resolved reconciliation with code {request.Body.ResolutionCode}.",
            cancellationToken);

        logger.LogInformation("Reconciliation {Id} resolved by {ActorId}.", request.Id, actorId);
        return Result<ReconciliationRecordDto>.Success(PaymentMapping.ToReconciliationDto(record));
    }
}
