using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Commands.MarkReconciliationInvestigating;
using Schoolera.Application.Payments.Commands.RequestPaymentRefund;
using Schoolera.Application.Payments.Commands.RequeryPaymentStatus;
using Schoolera.Application.Payments.Commands.ResolveReconciliation;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Payments.Queries.ListPaymentMonitoring;
using Schoolera.Application.Payments.Queries.ListReconciliationRecords;

namespace Schoolera.Api.Controllers;

[Route("api/admin/payments")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminPaymentsController(
    ISender mediator,
    ILogger<AdminPaymentsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("monitoring")]
    public Task<ActionResult<Result<IReadOnlyList<AdminPaymentMonitoringDto>>>> Monitoring(
        [FromQuery] int? status,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default) =>
        SendAsync(new ListPaymentMonitoringQuery(status, take), cancellationToken);

    [HttpGet("reconciliation")]
    public Task<ActionResult<Result<IReadOnlyList<ReconciliationRecordDto>>>> Reconciliation(
        [FromQuery] int? status,
        CancellationToken cancellationToken = default) =>
        SendAsync(new ListReconciliationRecordsQuery(status), cancellationToken);

    [HttpPost("reconciliation/{id:guid}/investigate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ReconciliationRecordDto>>> Investigate(
        Guid id,
        [FromBody] InvestigateReconciliationBody body,
        CancellationToken cancellationToken) =>
        SendAsync(new MarkReconciliationInvestigatingCommand(id, body.Note), cancellationToken);

    [HttpPost("reconciliation/{id:guid}/resolve")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ReconciliationRecordDto>>> Resolve(
        Guid id,
        [FromBody] ResolveReconciliationRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ResolveReconciliationCommand(id, body), cancellationToken);

    [HttpPost("intents/{intentId:guid}/refund")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PaymentIntentDto>>> Refund(
        Guid intentId,
        [FromBody] RequestPaymentRefundRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new RequestPaymentRefundCommand(intentId, body), cancellationToken);

    [HttpPost("intents/{intentId:guid}/requery")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PaymentIntentDto>>> Requery(
        Guid intentId,
        CancellationToken cancellationToken) =>
        SendAsync(new RequeryPaymentStatusCommand(intentId), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
