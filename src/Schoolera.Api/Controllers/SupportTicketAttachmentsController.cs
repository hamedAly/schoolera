using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.SupportTickets.Queries.DownloadSupportTicketAttachment;

namespace Schoolera.Api.Controllers;

[Route("api/support/tickets")]
[Authorize(Policy = SchooleraPolicies.SupportOrAdmin)]
public sealed class SupportTicketAttachmentsController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("{ticketId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DownloadSupportTicketAttachmentQuery(ticketId, attachmentId),
            cancellationToken);
        return result.ToSupportTicketFileDownloadResult(this);
    }
}
