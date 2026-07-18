using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Commands.CreateSupportTicket;
using Schoolera.Application.SupportTickets.Commands.ReopenSupportTicket;
using Schoolera.Application.SupportTickets.Commands.ReplyToSupportTicketAsParent;
using Schoolera.Application.SupportTickets.Commands.UploadSupportTicketAttachmentAsParent;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Queries.GetParentSupportTicket;
using Schoolera.Application.SupportTickets.Queries.ListParentSupportTicketHistory;
using Schoolera.Application.SupportTickets.Queries.ListParentSupportTickets;

namespace Schoolera.Api.Controllers;

[Route("api/parent/support-tickets")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentSupportTicketsController(
    ISender mediator,
    ILogger<ParentSupportTicketsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<PagedResult<SupportTicketListItemDto>>>> List(
        [FromQuery] int? status,
        [FromQuery] int? priority,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            ListParentSupportTicketsQuery.FromFilters(status, priority, pageNumber, pageSize),
            cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketParentDetailDto>>> Create(
        [FromBody] CreateSupportTicketRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new CreateSupportTicketCommand(body), cancellationToken);

    [HttpGet("{ticketId:guid}")]
    public Task<ActionResult<Result<SupportTicketParentDetailDto>>> Get(
        Guid ticketId,
        CancellationToken cancellationToken) =>
        SendAsync(new GetParentSupportTicketQuery(ticketId), cancellationToken);

    [HttpGet("{ticketId:guid}/history")]
    public Task<ActionResult<Result<IReadOnlyList<SupportTicketHistoryDto>>>> History(
        Guid ticketId,
        CancellationToken cancellationToken) =>
        SendAsync(new ListParentSupportTicketHistoryQuery(ticketId), cancellationToken);

    [HttpPost("{ticketId:guid}/replies")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketParentDetailDto>>> Reply(
        Guid ticketId,
        [FromBody] SupportTicketReplyRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ReplyToSupportTicketAsParentCommand(ticketId, body), cancellationToken);

    [HttpPost("{ticketId:guid}/reopen")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketParentDetailDto>>> Reopen(
        Guid ticketId,
        CancellationToken cancellationToken) =>
        SendAsync(new ReopenSupportTicketCommand(ticketId), cancellationToken);

    [HttpPost("{ticketId:guid}/attachments")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Result<SupportTicketParentDetailDto>>> UploadAttachment(
        Guid ticketId,
        IFormFile file,
        [FromForm] Guid? messageId = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        return FromResult(await Mediator.Send(UploadSupportTicketAttachmentAsParentCommand.FromRaw(ticketId, stream, file.FileName, file.ContentType, file.Length, messageId), cancellationToken));
    }

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
