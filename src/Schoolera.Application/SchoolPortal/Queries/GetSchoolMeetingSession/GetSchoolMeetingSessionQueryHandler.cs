using MediatR;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Meetings;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Enums;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Microsoft.Extensions.Logging;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolMeetingSession;

public sealed record GetSchoolMeetingSessionQuery(
    Guid SchoolId, Guid AppointmentId, SlotKind Kind)
    : IRequest<Result<SchoolMeetingSessionContextDto>>
{
    public static GetSchoolMeetingSessionQuery From(
        Guid schoolId, Guid appointmentId, int kind) =>
        new(schoolId, appointmentId, (SlotKind)kind);
}

public sealed class GetSchoolMeetingSessionQueryHandler(
    ISchoolPortalAccess portalAccess,
    IMeetingSessionService meetingSessions,
    IStringLocalizer<AdmissionMessages> localizer,
    ILogger<GetSchoolMeetingSessionQueryHandler> logger)
    : IRequestHandler<GetSchoolMeetingSessionQuery, Result<SchoolMeetingSessionContextDto>>
{
    public async Task<Result<SchoolMeetingSessionContextDto>> Handle(
        GetSchoolMeetingSessionQuery request, CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null ||
            !SchoolPortalPermissionMatrix.HasPermission(
                access.Data.IsOwner, access.Data.MembershipRole,
                SchoolPortalPermission.ManageApplicationReview))
            return Result<SchoolMeetingSessionContextDto>.Failure(
                [localizer["MeetingNotFound"]], [AdmissionErrorCodes.MeetingNotFound]);
        var context = await meetingSessions.GetSchoolContextAsync(
            request.SchoolId, access.Data.UserId, request.AppointmentId,
            request.Kind, cancellationToken);
        if (context is null)
        {
            logger.LogInformation(
                "School meeting context unavailable for appointment {AppointmentId}.",
                request.AppointmentId);
            return Result<SchoolMeetingSessionContextDto>.Failure(
                [localizer["MeetingNotFound"]], [AdmissionErrorCodes.MeetingNotFound]);
        }
        return Result<SchoolMeetingSessionContextDto>.Success(context);
    }
}
