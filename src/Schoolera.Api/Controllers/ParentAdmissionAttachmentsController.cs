using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Queries.DownloadAdmissionAttachment;
using Schoolera.Application.Auth.Constants;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Streams private admission attachments to their owning parent. Kept separate from
/// <see cref="ParentAdmissionApplicationsController"/> because file streaming returns a file result
/// rather than the standard <c>Result&lt;T&gt;</c> envelope.
/// </summary>
[Route("api/parent/admission-applications")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentAdmissionAttachmentsController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("{applicationId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DownloadAdmissionAttachmentQuery(applicationId, attachmentId),
            cancellationToken);
        return result.ToAdmissionFileDownloadResult(this);
    }
}
