using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Commands.CloseContactRequest;
using Schoolera.Application.Cms.Commands.ResolveContactRequest;
using Schoolera.Application.Cms.Commands.StartContactRequestReview;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Cms.Queries.GetContactRequestById;
using Schoolera.Application.Cms.Queries.ListContactRequests;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/contact-requests")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminContactRequestsController(
    ISender mediator,
    ILogger<AdminContactRequestsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<PagedResult<ContactRequestListItemDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            new ListContactRequestsQuery(search, status, category, pageNumber, pageSize),
            cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ActionResult<Result<ContactRequestDetailDto>>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new GetContactRequestByIdQuery(id), cancellationToken);

    [HttpPost("{id:guid}/start-review")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ContactRequestDetailDto>>> StartReview(
        Guid id,
        [FromBody] ContactAdminNoteBody? body,
        CancellationToken cancellationToken) =>
        SendAsync(new StartContactRequestReviewCommand(id, body?.AdminNote), cancellationToken);

    [HttpPost("{id:guid}/resolve")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ContactRequestDetailDto>>> Resolve(
        Guid id,
        [FromBody] ContactAdminNoteBody? body,
        CancellationToken cancellationToken) =>
        SendAsync(new ResolveContactRequestCommand(id, body?.AdminNote), cancellationToken);

    [HttpPost("{id:guid}/close")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ContactRequestDetailDto>>> Close(
        Guid id,
        [FromBody] ContactAdminNoteBody? body,
        CancellationToken cancellationToken) =>
        SendAsync(new CloseContactRequestCommand(id, body?.AdminNote), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
