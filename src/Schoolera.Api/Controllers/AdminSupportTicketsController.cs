using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Commands.AssignSupportTicket;
using Schoolera.Application.SupportTickets.Commands.ConvertContactRequestToSupportTicket;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Queries.GetAdminSupportTicketDetail;
using Schoolera.Application.SupportTickets.Queries.ListAdminSupportTickets;

namespace Schoolera.Api.Controllers;

[Route("api/admin/support-tickets")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminSupportTicketsController(
    ISender mediator,
    ILogger<AdminSupportTicketsController> logger) : ApiControllerBase(mediator, logger)
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
            ListAdminSupportTicketsQuery.FromFilters(
                search, status, priority, category, assignedSupportAgentUserId,
                unassignedOnly, firstResponseOverdueOnly, resolutionOverdueOnly,
                pageNumber, pageSize),
            cancellationToken);

    [HttpGet("{ticketId:guid}")]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> Get(
        Guid ticketId,
        CancellationToken cancellationToken) =>
        SendAsync(new GetAdminSupportTicketDetailQuery(ticketId), cancellationToken);

    [HttpPost("{ticketId:guid}/assign")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> Assign(
        Guid ticketId,
        [FromBody] SupportTicketAssignRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new AssignSupportTicketCommand(ticketId, body), cancellationToken);

    [HttpPost("convert-contact/{contactRequestId:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<SupportTicketSupportDetailDto>>> ConvertContact(
        Guid contactRequestId,
        CancellationToken cancellationToken) =>
        SendAsync(new ConvertContactRequestToSupportTicketCommand(contactRequestId), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
