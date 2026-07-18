using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Parent.Queries.DownloadChildDocument;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Streams private child vault documents to their owning parent. Kept separate from
/// <see cref="ParentChildDocumentsController"/> because file streaming returns a file result
/// rather than the standard <c>Result&lt;T&gt;</c> envelope.
/// </summary>
[Route("api/parent/children/{childId:guid}/documents")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentChildDocumentsDownloadController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid childId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DownloadChildDocumentQuery(childId, documentId),
            cancellationToken);
        return result.ToAdmissionFileDownloadResult(this);
    }
}
