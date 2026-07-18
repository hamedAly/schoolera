using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.SupportTickets.Queries.DownloadParentSupportTicketAttachment;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Streams parent-visible support ticket attachments. Kept separate because downloads
/// return a file result rather than the standard <c>Result&lt;T&gt;</c> envelope.
/// </summary>
[Route("api/parent/support-tickets")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentSupportTicketAttachmentsController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("{ticketId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DownloadParentSupportTicketAttachmentQuery(ticketId, attachmentId),
            cancellationToken);
        return result.ToSupportTicketFileDownloadResult(this);
    }
}
