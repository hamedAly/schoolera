using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Commands.CreateParentAdmissionSubscription;
using Schoolera.Application.Notifications.Commands.UnsubscribeParentAdmissionSubscription;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Notifications.Queries.ListParentAdmissionSubscriptions;

namespace Schoolera.Api.Controllers;

[Route("api/parent/admission-open-subscriptions")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentAdmissionOpenSubscriptionsController(
    ISender mediator,
    ILogger<ParentAdmissionOpenSubscriptionsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<IReadOnlyList<ParentAdmissionOpenSubscriptionDto>>>> List(
        CancellationToken cancellationToken) =>
        SendAsync(new ListParentAdmissionSubscriptionsQuery(), cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ParentAdmissionOpenSubscriptionDto>>> Create(
        [FromBody] CreateParentAdmissionOpenSubscriptionRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new CreateParentAdmissionSubscriptionCommand(body), cancellationToken);

    [HttpPost("{subscriptionId:guid}/unsubscribe")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ParentAdmissionOpenSubscriptionDto>>> Unsubscribe(
        Guid subscriptionId,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UnsubscribeParentAdmissionSubscriptionCommand(subscriptionId),
            cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
