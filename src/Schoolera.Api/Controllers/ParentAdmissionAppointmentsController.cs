using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Commands.CancelParentAppointment;
using Schoolera.Application.Admissions.Commands.ConfirmParentAppointment;
using Schoolera.Application.Admissions.Commands.JoinParentAppointment;
using Schoolera.Application.Admissions.Commands.RequestParentAppointmentReschedule;
using Schoolera.Application.Admissions.Commands.SelectParentAppointmentSlot;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Meetings;

namespace Schoolera.Api.Controllers;

[Route("api/parent/admission-applications/{applicationId:guid}/appointments/{kind:int}")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentAdmissionAppointmentsController(
    ISender mediator, ILogger<ParentAdmissionAppointmentsController> logger)
    : ApiControllerBase(mediator, logger)
{
    [HttpPost("confirm")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionAppointmentDto>>> Confirm(
        Guid applicationId, int kind, [FromBody] ParentAppointmentMutationRequest body,
        CancellationToken ct) =>
        FromResult(await Mediator.Send(
            ConfirmParentAppointmentCommand.From(applicationId, kind, body), ct));

    [HttpPost("select-slot")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionAppointmentDto>>> SelectSlot(
        Guid applicationId, int kind, [FromBody] ParentSelectAppointmentSlotRequest body,
        CancellationToken ct) =>
        FromResult(await Mediator.Send(
            SelectParentAppointmentSlotCommand.From(applicationId, kind, body), ct));

    [HttpPost("request-reschedule")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionAppointmentDto>>> RequestReschedule(
        Guid applicationId, int kind,
        [FromBody] ParentRequestAppointmentRescheduleRequest body,
        CancellationToken ct) =>
        FromResult(await Mediator.Send(
            RequestParentAppointmentRescheduleCommand.From(applicationId, kind, body), ct));

    [HttpPost("cancel")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionAppointmentDto>>> Cancel(
        Guid applicationId, int kind, [FromBody] ParentCancelAppointmentRequest body,
        CancellationToken ct) =>
        FromResult(await Mediator.Send(
            CancelParentAppointmentCommand.From(applicationId, kind, body), ct));

    [HttpPost("join")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MeetingAccessActionDto>>> Join(
        Guid applicationId, int kind, [FromBody] ParentJoinAppointmentRequest body,
        CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers.Pragma = "no-cache";
        return FromResult(await Mediator.Send(
            JoinParentAppointmentCommand.From(applicationId, kind, body), ct));
    }
}
