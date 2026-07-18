using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.CancelInterviewAssessmentSlot;
using Schoolera.Application.SchoolPortal.Commands.CloseInterviewAssessmentSlot;
using Schoolera.Application.SchoolPortal.Commands.CreateInterviewAssessmentSlot;
using Schoolera.Application.SchoolPortal.Commands.GenerateInterviewAssessmentSlotRecurrence;
using Schoolera.Application.SchoolPortal.Commands.OpenInterviewAssessmentSlot;
using Schoolera.Application.SchoolPortal.Commands.ReopenInterviewAssessmentSlot;
using Schoolera.Application.SchoolPortal.Commands.UpdateInterviewAssessmentSlot;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetInterviewAssessmentSlot;
using Schoolera.Application.SchoolPortal.Queries.ListInterviewAssessmentSlotAudit;
using Schoolera.Application.SchoolPortal.Queries.ListInterviewAssessmentSlots;
using Schoolera.Application.SchoolPortal.Queries.PreviewInterviewAssessmentSlotRecurrence;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/interview-assessment-slots")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalInterviewAssessmentSlotsController(
    ISender mediator, ILogger<SchoolPortalInterviewAssessmentSlotsController> logger)
    : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<InterviewAssessmentSlotDto>>>> List(
        Guid schoolId, [FromQuery] Guid? branchId, [FromQuery] Guid? stageId, [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId, [FromQuery] int? kind, [FromQuery] int? mode,
        [FromQuery] int? status, [FromQuery] Guid? resourceId, [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to, CancellationToken ct) =>
        FromResult(await Mediator.Send(ListInterviewAssessmentSlotsQuery.FromFilters(
            schoolId, branchId, stageId, gradeId, academicYearId, kind, mode, status,
            resourceId, from, to), ct));

    [HttpGet("{slotId:guid}")]
    public async Task<ActionResult<Result<InterviewAssessmentSlotDto>>> Get(Guid schoolId, Guid slotId,
        CancellationToken ct) => FromResult(await Mediator.Send(new GetInterviewAssessmentSlotQuery(schoolId, slotId), ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<InterviewAssessmentSlotDto>>> Create(Guid schoolId,
        [FromBody] UpsertInterviewAssessmentSlotRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CreateInterviewAssessmentSlotCommand(schoolId, body), ct));

    [HttpPut("{slotId:guid}"), ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<InterviewAssessmentSlotDto>>> Update(Guid schoolId, Guid slotId,
        [FromBody] UpsertInterviewAssessmentSlotRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpdateInterviewAssessmentSlotCommand(schoolId, slotId, body), ct));

    [HttpPost("{slotId:guid}/open"), ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<InterviewAssessmentSlotDto>>> Open(Guid schoolId, Guid slotId,
        [FromBody] byte[]? rowVersion, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            new OpenInterviewAssessmentSlotCommand(schoolId, slotId, rowVersion), ct));

    [HttpPost("{slotId:guid}/close"), ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<InterviewAssessmentSlotDto>>> Close(Guid schoolId, Guid slotId,
        [FromBody] byte[]? rowVersion, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            new CloseInterviewAssessmentSlotCommand(schoolId, slotId, rowVersion), ct));

    [HttpPost("{slotId:guid}/reopen"), ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<InterviewAssessmentSlotDto>>> Reopen(Guid schoolId, Guid slotId,
        [FromBody] byte[]? rowVersion, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            new ReopenInterviewAssessmentSlotCommand(schoolId, slotId, rowVersion), ct));

    [HttpPost("{slotId:guid}/cancel"), ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<InterviewAssessmentSlotDto>>> Cancel(Guid schoolId, Guid slotId,
        [FromBody] CancelInterviewAssessmentSlotRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new CancelInterviewAssessmentSlotCommand(schoolId, slotId, body), ct));

    [HttpPost("recurrence/preview"), ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SlotRecurrencePreviewDto>>> Preview(Guid schoolId,
        [FromBody] SlotRecurrenceRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new PreviewInterviewAssessmentSlotRecurrenceQuery(schoolId, body), ct));

    [HttpPost("recurrence/generate"), ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SlotGenerationResultDto>>> Generate(Guid schoolId,
        [FromBody] SlotRecurrenceRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new GenerateInterviewAssessmentSlotRecurrenceCommand(schoolId, body), ct));

    [HttpGet("{slotId:guid}/audit")]
    public async Task<ActionResult<Result<IReadOnlyList<InterviewAssessmentSlotAuditDto>>>> Audit(
        Guid schoolId, Guid slotId, CancellationToken ct) =>
        FromResult(await Mediator.Send(new ListInterviewAssessmentSlotAuditQuery(schoolId, slotId), ct));
}
