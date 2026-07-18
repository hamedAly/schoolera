using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Constants;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Application.Payments.Commands.MarkReconciliationInvestigating;

public sealed record MarkReconciliationInvestigatingCommand(Guid Id, string? Note)
    : IRequest<Result<ReconciliationRecordDto>>;

public sealed class MarkReconciliationInvestigatingCommandHandler(
    ICurrentUser currentUser,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    ILogger<MarkReconciliationInvestigatingCommandHandler> logger)
    : IRequestHandler<MarkReconciliationInvestigatingCommand, Result<ReconciliationRecordDto>>
{
    public async Task<Result<ReconciliationRecordDto>> Handle(
        MarkReconciliationInvestigatingCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<ReconciliationRecordDto>.Failure(
                ["Forbidden."], [PaymentErrorCodes.Forbidden]);
        }

        var record = await paymentRepository.GetReconciliationAsync(request.Id, cancellationToken);
        if (record is null)
        {
            return Result<ReconciliationRecordDto>.Failure(
                ["Not found."], [PaymentErrorCodes.ReconciliationNotFound]);
        }

        record.MarkInvestigating(request.Note);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reconciliation {Id} marked investigating.", request.Id);
        return Result<ReconciliationRecordDto>.Success(PaymentMapping.ToReconciliationDto(record));
    }
}
