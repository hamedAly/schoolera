using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Commands.UpdateParentNotificationPreferences;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Notifications.Queries.GetParentChannelAvailability;
using Schoolera.Application.Notifications.Queries.GetParentNotificationPreferences;

namespace Schoolera.Api.Controllers;

[Route("api/parent/notification-preferences")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentNotificationPreferencesController(
    ISender mediator,
    ILogger<ParentNotificationPreferencesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<ParentNotificationPreferenceDto>>> Get(
        CancellationToken cancellationToken) =>
        SendAsync(new GetParentNotificationPreferencesQuery(), cancellationToken);

    [HttpPut]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ParentNotificationPreferenceDto>>> Update(
        [FromBody] UpdateParentNotificationPreferencesRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new UpdateParentNotificationPreferencesCommand(body), cancellationToken);

    [HttpGet("channel-availability")]
    public Task<ActionResult<Result<ChannelAvailabilityDto>>> ChannelAvailability(
        CancellationToken cancellationToken) =>
        SendAsync(new GetParentChannelAvailabilityQuery(), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
