using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Commands.AcceptSchoolAdmissionApplication;
using Schoolera.Application.Admissions.Commands.GrantAdmissionAgeEligibilityException;
using Schoolera.Application.Admissions.Commands.CancelAdmissionAssessment;
using Schoolera.Application.Admissions.Commands.CancelAdmissionInterview;
using Schoolera.Application.Admissions.Commands.CompleteAdmissionAssessment;
using Schoolera.Application.Admissions.Commands.CompleteAdmissionInterview;
using Schoolera.Application.Admissions.Commands.MarkAdmissionRegistered;
using Schoolera.Application.Admissions.Commands.MoveAdmissionToWaitingList;
using Schoolera.Application.Admissions.Commands.RejectSchoolAdmissionApplication;
using Schoolera.Application.Admissions.Commands.RequestMissingDocuments;
using Schoolera.Application.Admissions.Commands.ReturnAdmissionFromWaitingList;
using Schoolera.Application.Admissions.Commands.ScheduleAdmissionAssessment;
using Schoolera.Application.Admissions.Commands.ScheduleAdmissionInterview;
using Schoolera.Application.Admissions.Commands.StartSchoolAdmissionReview;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Admissions.Queries.GetSchoolAdmissionApplication;
using Schoolera.Application.Admissions.Queries.ListSchoolAdmissionApplications;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/applications")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalAdmissionApplicationsController(
    ISender mediator,
    ILogger<SchoolPortalAdmissionApplicationsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<SchoolAdmissionApplicationListItemDto>>>> List(
        Guid schoolId,
        [FromQuery] int? status,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? educationalStageId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] string? sort,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return FromResult(await Mediator.Send(
            ListSchoolAdmissionApplicationsQuery.FromFilters(
                schoolId, status, branchId, gradeId, educationalStageId, academicYearId,
                search, dateFrom, dateTo, sort, pageNumber, pageSize),
            cancellationToken));
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> Get(
        Guid schoolId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetSchoolAdmissionApplicationQuery(schoolId, applicationId),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/start-review")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> StartReview(
        Guid schoolId,
        Guid applicationId,
        [FromBody] StartSchoolAdmissionReviewRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new StartSchoolAdmissionReviewCommand(
                schoolId,
                applicationId,
                body ?? new StartSchoolAdmissionReviewRequest(null, null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/request-missing-documents")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> RequestMissingDocuments(
        Guid schoolId,
        Guid applicationId,
        [FromBody] RequestMissingDocumentsRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new RequestMissingDocumentsCommand(schoolId, applicationId, body),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/schedule-interview")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> ScheduleInterview(
        Guid schoolId,
        Guid applicationId,
        [FromBody] ScheduleAdmissionAppointmentRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ScheduleAdmissionInterviewCommand(schoolId, applicationId, body, IsReschedule: false),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/reschedule-interview")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> RescheduleInterview(
        Guid schoolId,
        Guid applicationId,
        [FromBody] ScheduleAdmissionAppointmentRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ScheduleAdmissionInterviewCommand(schoolId, applicationId, body, IsReschedule: true),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/cancel-interview")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> CancelInterview(
        Guid schoolId,
        Guid applicationId,
        [FromBody] CancelAdmissionAppointmentRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CancelAdmissionInterviewCommand(
                schoolId,
                applicationId,
                body ?? new CancelAdmissionAppointmentRequest(null, null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/complete-interview")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> CompleteInterview(
        Guid schoolId,
        Guid applicationId,
        [FromBody] CompleteAdmissionAppointmentRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CompleteAdmissionInterviewCommand(schoolId, applicationId, body),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/schedule-assessment")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> ScheduleAssessment(
        Guid schoolId,
        Guid applicationId,
        [FromBody] ScheduleAdmissionAppointmentRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ScheduleAdmissionAssessmentCommand(schoolId, applicationId, body, IsReschedule: false),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/reschedule-assessment")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> RescheduleAssessment(
        Guid schoolId,
        Guid applicationId,
        [FromBody] ScheduleAdmissionAppointmentRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ScheduleAdmissionAssessmentCommand(schoolId, applicationId, body, IsReschedule: true),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/cancel-assessment")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> CancelAssessment(
        Guid schoolId,
        Guid applicationId,
        [FromBody] CancelAdmissionAppointmentRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CancelAdmissionAssessmentCommand(
                schoolId,
                applicationId,
                body ?? new CancelAdmissionAppointmentRequest(null, null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/complete-assessment")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> CompleteAssessment(
        Guid schoolId,
        Guid applicationId,
        [FromBody] CompleteAdmissionAppointmentRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CompleteAdmissionAssessmentCommand(schoolId, applicationId, body),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/move-to-waiting-list")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> MoveToWaitingList(
        Guid schoolId,
        Guid applicationId,
        [FromBody] MoveToWaitingListRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new MoveAdmissionToWaitingListCommand(
                schoolId,
                applicationId,
                body ?? new MoveToWaitingListRequest(null, null, null, null, null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/return-from-waiting-list")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> ReturnFromWaitingList(
        Guid schoolId,
        Guid applicationId,
        [FromBody] ReturnFromWaitingListRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ReturnAdmissionFromWaitingListCommand(
                schoolId,
                applicationId,
                body ?? new ReturnFromWaitingListRequest(null, null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/accept")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> Accept(
        Guid schoolId,
        Guid applicationId,
        [FromBody] AcceptSchoolAdmissionApplicationRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new AcceptSchoolAdmissionApplicationCommand(
                schoolId,
                applicationId,
                body ?? new AcceptSchoolAdmissionApplicationRequest(null, null)),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/age-eligibility-exception")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> GrantAgeEligibilityException(
        Guid schoolId,
        Guid applicationId,
        [FromBody] GrantAdmissionAgeEligibilityExceptionRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GrantAdmissionAgeEligibilityExceptionCommand(schoolId, applicationId, body),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> Reject(
        Guid schoolId,
        Guid applicationId,
        [FromBody] RejectSchoolAdmissionApplicationRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new RejectSchoolAdmissionApplicationCommand(schoolId, applicationId, body),
            cancellationToken));
    }

    [HttpPost("{applicationId:guid}/mark-registered")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdmissionApplicationDetailDto>>> MarkRegistered(
        Guid schoolId,
        Guid applicationId,
        [FromBody] MarkRegisteredRequest? body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new MarkAdmissionRegisteredCommand(
                schoolId,
                applicationId,
                body ?? new MarkRegisteredRequest(null, null)),
            cancellationToken));
    }
}
