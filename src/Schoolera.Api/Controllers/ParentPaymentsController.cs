using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Commands.CreatePaymentIntent;
using Schoolera.Application.Payments.Commands.ProcessPaymentReturn;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Payments.Queries.GetPaymentIntent;
using Schoolera.Application.Payments.Queries.GetPaymentReceipt;
using Schoolera.Application.Payments.Queries.GetPaymentSummary;
using Schoolera.Application.Payments.Queries.ListParentPayableItems;
using Schoolera.Application.Payments.Queries.ListParentPaymentIntents;

namespace Schoolera.Api.Controllers;

[Route("api/parent/payments")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentPaymentsController(
    ISender mediator,
    ILogger<ParentPaymentsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("payable-items")]
    public Task<ActionResult<Result<IReadOnlyList<ParentPayableItemDto>>>> ListPayableItems(
        CancellationToken cancellationToken) =>
        SendAsync(new ListParentPayableItemsQuery(), cancellationToken);

    [HttpGet("summary/{payableItemId:guid}")]
    public Task<ActionResult<Result<PaymentSummaryDto>>> Summary(
        Guid payableItemId,
        [FromQuery] Guid? admissionApplicationId,
        CancellationToken cancellationToken) =>
        SendAsync(new GetPaymentSummaryQuery(payableItemId, admissionApplicationId), cancellationToken);

    [HttpGet]
    public Task<ActionResult<Result<IReadOnlyList<PaymentIntentDto>>>> List(
        CancellationToken cancellationToken) =>
        SendAsync(new ListParentPaymentIntentsQuery(), cancellationToken);

    [HttpGet("{intentId:guid}")]
    public Task<ActionResult<Result<PaymentIntentDto>>> Get(
        Guid intentId,
        CancellationToken cancellationToken) =>
        SendAsync(new GetPaymentIntentQuery(intentId), cancellationToken);

    [HttpGet("{intentId:guid}/receipt")]
    public Task<ActionResult<Result<PaymentReceiptDto>>> Receipt(
        Guid intentId,
        CancellationToken cancellationToken) =>
        SendAsync(new GetPaymentReceiptQuery(intentId), cancellationToken);

    [HttpPost("intents")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PaymentIntentDto>>> CreateIntent(
        [FromBody] CreatePaymentIntentRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new CreatePaymentIntentCommand(body), cancellationToken);

    [HttpPost("return")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PaymentIntentDto>>> ProcessReturn(
        [FromBody] PaymentReturnRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ProcessPaymentReturnCommand(body.Reference), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
