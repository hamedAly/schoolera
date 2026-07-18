using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Commands.CreateFinancingRequest;
using Schoolera.Application.Payments.Commands.SelectFinancingOffer;
using Schoolera.Application.Payments.Dtos;
using Schoolera.Application.Payments.Queries.GetFinancingRequest;
using Schoolera.Application.Payments.Queries.ListParentFinancingRequests;

namespace Schoolera.Api.Controllers;

[Route("api/parent/financing")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentFinancingController(
    ISender mediator,
    ILogger<ParentFinancingController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<IReadOnlyList<FinancingRequestDto>>>> List(
        CancellationToken cancellationToken) =>
        SendAsync(new ListParentFinancingRequestsQuery(), cancellationToken);

    [HttpGet("{requestId:guid}")]
    public Task<ActionResult<Result<FinancingRequestDto>>> Get(
        Guid requestId,
        CancellationToken cancellationToken) =>
        SendAsync(new GetFinancingRequestQuery(requestId), cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FinancingRequestDto>>> Create(
        [FromBody] CreateFinancingRequestBody body,
        CancellationToken cancellationToken) =>
        SendAsync(new CreateFinancingRequestCommand(body), cancellationToken);

    [HttpPost("{requestId:guid}/select-offer")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FinancingRequestDto>>> SelectOffer(
        Guid requestId,
        [FromBody] SelectFinancingOfferRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new SelectFinancingOfferCommand(requestId, body), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
