using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Commands.UpsertSchoolPayableItem;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Payments.Queries.ListSchoolPayableItems;
using Schoolera.Application.Payments.Queries.ListSchoolSettlementPayments;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/payments")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalPaymentsController(
    ISender mediator,
    ILogger<SchoolPortalPaymentsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("payable-items")]
    public Task<ActionResult<Result<IReadOnlyList<SchoolPayableItemAdminDto>>>> ListPayableItems(
        Guid schoolId,
        CancellationToken cancellationToken) =>
        SendAsync(new ListSchoolPayableItemsQuery(schoolId), cancellationToken);

    [HttpPut("payable-items")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SchoolPayableItemAdminDto>>> UpsertPayableItem(
        Guid schoolId,
        [FromBody] UpsertSchoolPayableItemRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new UpsertSchoolPayableItemCommand(schoolId, body), cancellationToken);

    [HttpGet("settlements")]
    public Task<ActionResult<Result<IReadOnlyList<SchoolSettlementPaymentDto>>>> Settlements(
        Guid schoolId,
        CancellationToken cancellationToken) =>
        SendAsync(new ListSchoolSettlementPaymentsQuery(schoolId), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
