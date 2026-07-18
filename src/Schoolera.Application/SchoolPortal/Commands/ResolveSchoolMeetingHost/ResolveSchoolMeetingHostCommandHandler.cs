using MediatR;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Meetings;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Enums;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Schoolera.Application.SchoolPortal.Commands.ResolveSchoolMeetingHost;

public sealed record ResolveSchoolMeetingHostCommand(
    Guid SchoolId, Guid AppointmentId, SlotKind Kind)
    : IRequest<Result<MeetingAccessActionDto>>
{
    public static ResolveSchoolMeetingHostCommand From(
        Guid schoolId, Guid appointmentId, int kind) =>
        new(schoolId, appointmentId, (SlotKind)kind);
}

public sealed class ResolveSchoolMeetingHostCommandHandler(
    ISchoolPortalAccess portalAccess,
    IMeetingSessionService meetingSessions,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AdmissionMessages> localizer,
    ILogger<ResolveSchoolMeetingHostCommandHandler> logger)
    : IRequestHandler<ResolveSchoolMeetingHostCommand, Result<MeetingAccessActionDto>>
{
    public async Task<Result<MeetingAccessActionDto>> Handle(
        ResolveSchoolMeetingHostCommand request, CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null ||
            !SchoolPortalPermissionMatrix.HasPermission(
                access.Data.IsOwner, access.Data.MembershipRole,
                SchoolPortalPermission.ManageApplicationReview))
            return Result<MeetingAccessActionDto>.Failure(
                [localizer["MeetingAccessUnavailable"]],
                [AdmissionErrorCodes.MeetingAccessUnavailable]);
        var action = await meetingSessions.ResolveSchoolHostAccessAsync(
            request.SchoolId, access.Data.UserId, request.AppointmentId,
            request.Kind, cancellationToken);
        if (action is null)
            return Result<MeetingAccessActionDto>.Failure(
                [localizer["MeetingAccessUnavailable"]],
                [AdmissionErrorCodes.MeetingAccessUnavailable]);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Issued School meeting host access for appointment {AppointmentId}.",
            request.AppointmentId);
        return Result<MeetingAccessActionDto>.Success(action);
    }
}
