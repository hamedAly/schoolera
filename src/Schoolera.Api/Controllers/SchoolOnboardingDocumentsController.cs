using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.SchoolOnboarding.Queries.DownloadMyDocument;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Streams private onboarding documents to their owning SchoolOwner. Kept separate from
/// <see cref="SchoolOnboardingController"/> because file streaming returns a file result
/// rather than the standard <c>Result&lt;T&gt;</c> envelope.
/// </summary>
[Route("api/school-onboarding/me/documents")]
[Authorize(Policy = SchooleraPolicies.SchoolOwnerOnly)]
public sealed class SchoolOnboardingDocumentsController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DownloadOwnerOnboardingDocumentQuery(documentId), cancellationToken);
        return result.ToFileDownloadResult(this);
    }
}
