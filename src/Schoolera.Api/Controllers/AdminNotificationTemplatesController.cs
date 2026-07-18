using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Commands.CreateNotificationTemplateVersion;
using Schoolera.Application.Integrations.Commands.PublishNotificationTemplateVersion;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Application.Integrations.Queries.ListNotificationTemplates;

namespace Schoolera.Api.Controllers;

[Route("api/admin/notification-templates")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminNotificationTemplatesController(
    ISender mediator,
    ILogger<AdminNotificationTemplatesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<IReadOnlyList<NotificationTemplateListItemDto>>>> List(
        CancellationToken cancellationToken) =>
        SendAsync(new ListNotificationTemplatesQuery(), cancellationToken);

    [HttpPost("versions")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<NotificationTemplateVersionDto>>> CreateVersion(
        [FromBody] CreateTemplateVersionRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new CreateNotificationTemplateVersionCommand(body), cancellationToken);

    [HttpPost("versions/{versionId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<NotificationTemplateVersionDto>>> PublishVersion(
        Guid versionId,
        CancellationToken cancellationToken) =>
        SendAsync(new PublishNotificationTemplateVersionCommand(versionId), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
