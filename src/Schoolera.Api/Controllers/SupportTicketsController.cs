using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Commands.AddSupportTicketInternalNote;
using Schoolera.Application.SupportTickets.Commands.AssignSupportTicketToMe;
using Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketCategory;
using Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketPriority;
using Schoolera.Application.SupportTickets.Commands.ChangeSupportTicketStatus;
using Schoolera.Application.SupportTickets.Commands.ReassignSupportTicket;
using Schoolera.Application.SupportTickets.Commands.ReplyToSupportTicketAsSupport;
using Schoolera.Application.SupportTickets.Commands.UnassignSupportTicket;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Queries.GetSupportTicketDetail;
using Schoolera.Application.SupportTickets.Queries.ListSupportTickets;

namespace Schoolera.Api.Controllers;

[Route("api/support/tickets")]
[Authorize(Policy = SchooleraPolicies.SupportOrAdmin)]
public sealed class SupportTicketsController(
    ISender mediator,
    ILogger<SupportTicketsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<PagedResult<SupportTicketListItemDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] int? status,
        [FromQuery] int? priority,
        [FromQuery] int? category,
        [FromQuery] Guid? assignedSupportAgentUserId,
        [FromQuery] bool? unassignedOnly,
        [FromQuery] bool? firstResponseOverdueOnly,
        [FromQuery] bool? resolutionOverdueOnly,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            ListSupportTicketsQuery.FromFilters(
                search, status, priority, category, assignedSupportAgentUserId,
                unassignedOnly, firstResponseOverdueOnly, resolutionOverdueOnly,
                pageNumber, pageSize),
            cancellationToken);

    [HttpGet("{ticketId:guid}")]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> Get(
        Guid ticketId,
        CancellationToken cancellationToken) =>
        SendAsync(new GetSupportTicketDetailQuery(ticketId), cancellationToken);

    [HttpPost("{ticketId:guid}/assign-to-me")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> AssignToMe(
        Guid ticketId,
        CancellationToken cancellationToken) =>
        SendAsync(new AssignSupportTicketToMeCommand(ticketId), cancellationToken);

    [HttpPost("{ticketId:guid}/reassign")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> Reassign(
        Guid ticketId,
        [FromBody] SupportTicketAssignRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ReassignSupportTicketCommand(ticketId, body), cancellationToken);

    [HttpPost("{ticketId:guid}/unassign")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> Unassign(
        Guid ticketId,
        CancellationToken cancellationToken) =>
        SendAsync(new UnassignSupportTicketCommand(ticketId), cancellationToken);

    [HttpPost("{ticketId:guid}/replies")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> Reply(
        Guid ticketId,
        [FromBody] SupportTicketReplyRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ReplyToSupportTicketAsSupportCommand(ticketId, body), cancellationToken);

    [HttpPost("{ticketId:guid}/internal-notes")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> InternalNote(
        Guid ticketId,
        [FromBody] SupportTicketReplyRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new AddSupportTicketInternalNoteCommand(ticketId, body), cancellationToken);

    [HttpPost("{ticketId:guid}/status")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> ChangeStatus(
        Guid ticketId,
        [FromBody] SupportTicketStatusChangeRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ChangeSupportTicketStatusCommand(ticketId, body), cancellationToken);

    [HttpPost("{ticketId:guid}/priority")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> ChangePriority(
        Guid ticketId,
        [FromBody] SupportTicketPriorityChangeRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ChangeSupportTicketPriorityCommand(ticketId, body), cancellationToken);

    [HttpPost("{ticketId:guid}/category")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> ChangeCategory(
        Guid ticketId,
        [FromBody] SupportTicketCategoryChangeRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ChangeSupportTicketCategoryCommand(ticketId, body), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
