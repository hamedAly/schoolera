using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Meetings;
using Schoolera.Application.SchoolPortal.Commands.ResolveSchoolMeetingHost;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolMeetingSession;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/{schoolId:guid}/meeting-sessions")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalMeetingSessionsController(
    ISender mediator,
    ILogger<SchoolPortalMeetingSessionsController> logger)
    : ApiControllerBase(mediator, logger)
{
    [HttpGet("appointments/{appointmentId:guid}")]
    public async Task<ActionResult<Result<SchoolMeetingSessionContextDto>>> Get(
        Guid schoolId, Guid appointmentId, [FromQuery] int kind,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(
            GetSchoolMeetingSessionQuery.From(schoolId, appointmentId, kind),
            cancellationToken));

    [HttpPost("appointments/{appointmentId:guid}/host")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<MeetingAccessActionDto>>> Host(
        Guid schoolId, Guid appointmentId, [FromQuery] int kind,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers.Pragma = "no-cache";
        return FromResult(await Mediator.Send(
            ResolveSchoolMeetingHostCommand.From(schoolId, appointmentId, kind),
            cancellationToken));
    }
}
