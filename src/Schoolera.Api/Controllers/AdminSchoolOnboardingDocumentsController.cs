using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admin.SchoolOnboarding.Queries.DownloadDocument;
using Schoolera.Application.Auth.Constants;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Streams private onboarding documents to PlatformAdmin reviewers. Kept separate from
/// <see cref="AdminSchoolOnboardingController"/> because file streaming returns a file result
/// rather than the standard <c>Result&lt;T&gt;</c> envelope.
/// </summary>
[Route("api/admin/school-onboarding")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminSchoolOnboardingDocumentsController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("{applicationId:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DownloadAdminOnboardingDocumentQuery(applicationId, documentId), cancellationToken);
        return result.ToFileDownloadResult(this);
    }
}
