using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Meetings;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.JoinParentAppointment;

public sealed record JoinParentAppointmentCommand(
    Guid ApplicationId, SlotKind Kind, ParentJoinAppointmentRequest Body)
    : IRequest<Result<MeetingAccessActionDto>>
{
    public static JoinParentAppointmentCommand From(
        Guid applicationId, int kind, ParentJoinAppointmentRequest body) =>
        new(applicationId, (SlotKind)kind, body);
}

public sealed class JoinParentAppointmentCommandHandler(
    ICurrentUser currentUser,
    IInterviewAssessmentSlotRepository repository,
    IMeetingSessionService meetingSessions,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AdmissionMessages> localizer,
    ILogger<JoinParentAppointmentCommandHandler> logger)
    : IRequestHandler<JoinParentAppointmentCommand, Result<MeetingAccessActionDto>>
{
    public async Task<Result<MeetingAccessActionDto>> Handle(
        JoinParentAppointmentCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId ||
            !currentUser.IsInRole(SchooleraRoles.Parent) ||
            !Enum.IsDefined(request.Kind))
            return AdmissionResults.NotFound<MeetingAccessActionDto>();
        var application = await repository.GetOwnedApplicationAsync(
            userId, request.ApplicationId, ct);
        if (application is null) return AdmissionResults.NotFound<MeetingAccessActionDto>();
        var appointment = request.Kind == SlotKind.Interview
            ? application.InterviewAppointments.OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault(x => x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed)
            : null;
        var assessment = request.Kind == SlotKind.Assessment
            ? application.AssessmentAppointments.OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault(x => x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed)
            : null;
        var appointmentId = appointment?.Id ?? assessment?.Id;
        var rowVersion = appointment?.RowVersion ?? assessment?.RowVersion;
        if (!appointmentId.HasValue || rowVersion is null ||
            !rowVersion.AsSpan().SequenceEqual(request.Body.RowVersion))
            return AdmissionResults.Failure<MeetingAccessActionDto>(
                localizer["AppointmentConcurrencyConflict"],
                AdmissionErrorCodes.AppointmentConcurrencyConflict);
        var action = await meetingSessions.ResolveParentAccessAsync(
            userId, application.Id, appointmentId.Value, request.Kind, ct);
        if (action is null)
            return AdmissionResults.Failure<MeetingAccessActionDto>(
                localizer["MeetingAccessUnavailable"],
                AdmissionErrorCodes.AppointmentJoinProviderUnavailable);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "Issued Parent meeting access for application {ApplicationId}.",
            application.Id);
        return Result<MeetingAccessActionDto>.Success(action);
    }
}
