using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolOnboarding.Commands.DeleteDocument;
using Schoolera.Application.SchoolOnboarding.Commands.ResubmitApplication;
using Schoolera.Application.SchoolOnboarding.Commands.SaveAuthorizedRepresentative;
using Schoolera.Application.SchoolOnboarding.Commands.SaveOrganization;
using Schoolera.Application.SchoolOnboarding.Commands.SavePrimaryBranch;
using Schoolera.Application.SchoolOnboarding.Commands.SaveSchoolDetails;
using Schoolera.Application.SchoolOnboarding.Commands.SubmitApplication;
using Schoolera.Application.SchoolOnboarding.Commands.UploadDocument;
using Schoolera.Application.SchoolOnboarding.Dtos;
using Schoolera.Application.SchoolOnboarding.Queries.GetDocumentTypes;
using Schoolera.Application.SchoolOnboarding.Queries.GetMyApplication;

namespace Schoolera.Api.Controllers;

[Route("api/school-onboarding")]
[Authorize(Policy = SchooleraPolicies.SchoolOwnerOnly)]
public sealed class SchoolOnboardingController(
    ISender mediator,
    ILogger<SchoolOnboardingController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("me")]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto?>>> GetMine(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetMyOnboardingApplicationQuery(), cancellationToken));
    }

    [HttpGet("document-types")]
    public async Task<ActionResult<Result<OnboardingDocumentTypesResponseDto>>> GetDocumentTypes(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetOnboardingDocumentTypesQuery(), cancellationToken));
    }

    [HttpPut("me/organization")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> SaveOrganization(
        [FromBody] SaveOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPut("me/authorized-representative")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> SaveAuthorizedRepresentative(
        [FromBody] SaveAuthorizedRepresentativeCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPut("me/school-details")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> SaveSchoolDetails(
        [FromBody] SaveSchoolDetailsCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPut("me/primary-branch")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> SavePrimaryBranch(
        [FromBody] SavePrimaryBranchCommand command,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("me/documents")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> UploadDocument(
        [FromForm] Guid documentTypeId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadOnboardingDocumentCommand(
            documentTypeId,
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length);
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpDelete("me/documents/{documentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> DeleteDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new DeleteOnboardingDocumentCommand(documentId), cancellationToken));
    }

    [HttpPost("me/submit")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> Submit(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new SubmitOnboardingApplicationCommand(), cancellationToken));
    }

    [HttpPost("me/resubmit")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MyOnboardingApplicationDto>>> Resubmit(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new ResubmitOnboardingApplicationCommand(), cancellationToken));
    }
}
