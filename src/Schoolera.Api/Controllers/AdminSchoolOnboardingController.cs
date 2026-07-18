using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admin.SchoolOnboarding.Commands.ApproveApplication;
using Schoolera.Application.Admin.SchoolOnboarding.Commands.RejectApplication;
using Schoolera.Application.Admin.SchoolOnboarding.Commands.RequestChanges;
using Schoolera.Application.Admin.SchoolOnboarding.Commands.StartReview;
using Schoolera.Application.Admin.SchoolOnboarding.Queries.GetApplicationDetail;
using Schoolera.Application.Admin.SchoolOnboarding.Queries.GetApplications;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Api.Controllers;

[Route("api/admin/school-onboarding")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminSchoolOnboardingController(
    ISender mediator,
    ILogger<AdminSchoolOnboardingController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AdminOnboardingListItemDto>>>> List(
        [FromQuery] string? status,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var query = new GetOnboardingApplicationsQuery(status, pageNumber, pageSize);
        return FromResult(await Mediator.Send(query, cancellationToken));
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<Result<AdminOnboardingDetailDto>>> Detail(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetOnboardingApplicationDetailQuery(applicationId), cancellationToken));
    }

    [HttpPost("{applicationId:guid}/start-review")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdminOnboardingDetailDto>>> StartReview(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new StartOnboardingReviewCommand(applicationId), cancellationToken));
    }

    [HttpPost("{applicationId:guid}/request-changes")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdminOnboardingDetailDto>>> RequestChanges(
        Guid applicationId,
        [FromBody] OnboardingReviewReasonInput body,
        CancellationToken cancellationToken)
    {
        var command = new RequestOnboardingChangesCommand(applicationId, body.OwnerVisibleReason, body.InternalNote);
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{applicationId:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdminOnboardingDetailDto>>> Approve(
        Guid applicationId,
        [FromBody] OnboardingApprovalInput body,
        CancellationToken cancellationToken)
    {
        var command = new ApproveOnboardingApplicationCommand(applicationId, body.InternalNote);
        return FromResult(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{applicationId:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdminOnboardingDetailDto>>> Reject(
        Guid applicationId,
        [FromBody] OnboardingReviewReasonInput body,
        CancellationToken cancellationToken)
    {
        var command = new RejectOnboardingApplicationCommand(applicationId, body.OwnerVisibleReason, body.InternalNote);
        return FromResult(await Mediator.Send(command, cancellationToken));
    }
}
