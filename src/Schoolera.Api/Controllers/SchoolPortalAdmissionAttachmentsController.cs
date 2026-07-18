using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Queries.DownloadSchoolAdmissionAttachment;
using Schoolera.Application.Auth.Constants;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Streams private admission attachments to school portal users with access to the school.
/// </summary>
[Route("api/school-portal/schools/{schoolId:guid}/applications")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalAdmissionAttachmentsController(ISender mediator) : ControllerBase
{
    private ISender Mediator { get; } = mediator;

    [HttpGet("{applicationId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid schoolId,
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DownloadSchoolAdmissionAttachmentQuery(schoolId, applicationId, attachmentId),
            cancellationToken);
        return result.ToAdmissionFileDownloadResult(this);
    }
}
