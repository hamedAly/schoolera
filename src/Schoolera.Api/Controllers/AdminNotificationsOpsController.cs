using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Application.Integrations.Queries.GetNotificationOpsSummary;

namespace Schoolera.Api.Controllers;

[Route("api/admin/notifications/ops")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminNotificationsOpsController(
    ISender mediator,
    ILogger<AdminNotificationsOpsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("summary")]
    public async Task<ActionResult<Result<NotificationOpsSummaryDto>>> GetSummary(
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(new GetNotificationOpsSummaryQuery(), cancellationToken));
}
