using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Commands.PublishHomepageContent;
using Schoolera.Application.Cms.Commands.UnpublishHomepageContent;
using Schoolera.Application.Cms.Commands.UpdateHomepageContent;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Cms.Queries.GetHomepageContent;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/cms/home")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminCmsHomeController(
    ISender mediator,
    ILogger<AdminCmsHomeController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<HomepageAdminDto>>> Get(CancellationToken cancellationToken) =>
        SendAsync(new GetHomepageContentQuery(), cancellationToken);

    [HttpPut]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<HomepageAdminDto>>> Update(
        [FromBody] UpdateHomepageContentBody body,
        CancellationToken cancellationToken) =>
        SendAsync(new UpdateHomepageContentCommand(body), cancellationToken);

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<HomepageAdminDto>>> Publish(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new PublishHomepageContentCommand(id), cancellationToken);

    [HttpPost("{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<HomepageAdminDto>>> Unpublish(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new UnpublishHomepageContentCommand(id), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
