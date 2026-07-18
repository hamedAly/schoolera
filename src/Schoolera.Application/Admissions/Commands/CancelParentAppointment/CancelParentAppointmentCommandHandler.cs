using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.CancelParentAppointment;

public sealed record CancelParentAppointmentCommand(
    Guid ApplicationId, SlotKind Kind, ParentCancelAppointmentRequest Body)
    : IRequest<Result<AdmissionAppointmentDto>>
{
    public static CancelParentAppointmentCommand From(
        Guid applicationId, int kind, ParentCancelAppointmentRequest body) =>
        new(applicationId, (SlotKind)kind, body);
}

public sealed class CancelParentAppointmentCommandHandler(
    ICurrentUser currentUser,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<CancelParentAppointmentCommandHandler> logger)
    : IRequestHandler<CancelParentAppointmentCommand, Result<AdmissionAppointmentDto>>
{
    public Task<Result<AdmissionAppointmentDto>> Handle(
        CancelParentAppointmentCommand request, CancellationToken ct) =>
        ParentAppointmentCommandSupport.ExecuteAsync(currentUser, repository, localizer,
            request.ApplicationId, request.Kind, ParentAppointmentActionKind.Cancel,
            null, request.Body.RowVersion, request.Body.IdempotencyKey, request.Body.Reason, ct);
}
