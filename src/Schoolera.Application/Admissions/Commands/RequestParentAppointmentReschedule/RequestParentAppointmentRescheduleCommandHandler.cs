using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.RequestParentAppointmentReschedule;

public sealed record RequestParentAppointmentRescheduleCommand(
    Guid ApplicationId, SlotKind Kind, ParentRequestAppointmentRescheduleRequest Body)
    : IRequest<Result<AdmissionAppointmentDto>>
{
    public static RequestParentAppointmentRescheduleCommand From(
        Guid applicationId, int kind, ParentRequestAppointmentRescheduleRequest body) =>
        new(applicationId, (SlotKind)kind, body);
}

public sealed class RequestParentAppointmentRescheduleCommandHandler(
    ICurrentUser currentUser,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<RequestParentAppointmentRescheduleCommandHandler> logger)
    : IRequestHandler<RequestParentAppointmentRescheduleCommand, Result<AdmissionAppointmentDto>>
{
    public Task<Result<AdmissionAppointmentDto>> Handle(
        RequestParentAppointmentRescheduleCommand request, CancellationToken ct) =>
        ParentAppointmentCommandSupport.ExecuteAsync(currentUser, repository, localizer,
            request.ApplicationId, request.Kind, ParentAppointmentActionKind.RequestReschedule,
            null, request.Body.RowVersion, request.Body.IdempotencyKey, request.Body.Reason, ct);
}
