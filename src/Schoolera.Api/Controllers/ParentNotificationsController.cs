using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Commands.MarkAllParentNotificationsRead;
using Schoolera.Application.Notifications.Commands.MarkParentNotificationRead;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Notifications.Queries.GetParentUnreadCount;
using Schoolera.Application.Notifications.Queries.ListParentNotifications;

namespace Schoolera.Api.Controllers;

[Route("api/parent/notifications")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentNotificationsController(
    ISender mediator,
    ILogger<ParentNotificationsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<PagedResult<ParentInAppNotificationDto>>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendAsync(new ListParentNotificationsQuery(pageNumber, pageSize), cancellationToken);

    [HttpGet("unread-count")]
    public Task<ActionResult<Result<int>>> UnreadCount(CancellationToken cancellationToken) =>
        SendAsync(new GetParentUnreadCountQuery(), cancellationToken);

    [HttpPost("{notificationId:guid}/read")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ParentInAppNotificationDto>>> MarkRead(
        Guid notificationId,
        CancellationToken cancellationToken) =>
        SendAsync(new MarkParentNotificationReadCommand(notificationId), cancellationToken);

    [HttpPost("read-all")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<int>>> MarkAllRead(CancellationToken cancellationToken) =>
        SendAsync(new MarkAllParentNotificationsReadCommand(), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
