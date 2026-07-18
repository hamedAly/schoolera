using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.ConfirmParentAppointment;

public sealed record ConfirmParentAppointmentCommand(
    Guid ApplicationId, SlotKind Kind, ParentAppointmentMutationRequest Body)
    : IRequest<Result<AdmissionAppointmentDto>>
{
    public static ConfirmParentAppointmentCommand From(
        Guid applicationId, int kind, ParentAppointmentMutationRequest body) =>
        new(applicationId, (SlotKind)kind, body);
}

public sealed class ConfirmParentAppointmentCommandHandler(
    ICurrentUser currentUser,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<ConfirmParentAppointmentCommandHandler> logger)
    : IRequestHandler<ConfirmParentAppointmentCommand, Result<AdmissionAppointmentDto>>
{
    public Task<Result<AdmissionAppointmentDto>> Handle(
        ConfirmParentAppointmentCommand request, CancellationToken ct) =>
        ParentAppointmentCommandSupport.ExecuteAsync(currentUser, repository, localizer,
            request.ApplicationId, request.Kind, ParentAppointmentActionKind.Confirm,
            null, request.Body.RowVersion, request.Body.IdempotencyKey, null, ct);
}
