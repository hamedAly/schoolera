using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Commands.ArchiveCmsPage;
using Schoolera.Application.Cms.Commands.CreateCmsPage;
using Schoolera.Application.Cms.Commands.PublishCmsPage;
using Schoolera.Application.Cms.Commands.UnpublishCmsPage;
using Schoolera.Application.Cms.Commands.UpdateCmsPage;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Cms.Queries.GetCmsPageById;
using Schoolera.Application.Cms.Queries.ListCmsPages;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/cms/pages")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminCmsPagesController(
    ISender mediator,
    ILogger<AdminCmsPagesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<PagedResult<CmsPageListItemDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendAsync(new ListCmsPagesQuery(search, status, pageNumber, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ActionResult<Result<CmsPageAdminDto>>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new GetCmsPageByIdQuery(id), cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CmsPageAdminDto>>> Create(
        [FromBody] CreateCmsPageCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CmsPageAdminDto>>> Update(
        Guid id,
        [FromBody] UpdateCmsPageBody body,
        CancellationToken cancellationToken) =>
        SendAsync(new UpdateCmsPageCommand(id, body), cancellationToken);

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CmsPageAdminDto>>> Publish(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new PublishCmsPageCommand(id), cancellationToken);

    [HttpPost("{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CmsPageAdminDto>>> Unpublish(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new UnpublishCmsPageCommand(id), cancellationToken);

    [HttpPost("{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CmsPageAdminDto>>> Archive(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new ArchiveCmsPageCommand(id), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
