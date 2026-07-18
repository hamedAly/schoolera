using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.SelectParentAppointmentSlot;

public sealed record SelectParentAppointmentSlotCommand(
    Guid ApplicationId, SlotKind Kind, ParentSelectAppointmentSlotRequest Body)
    : IRequest<Result<AdmissionAppointmentDto>>
{
    public static SelectParentAppointmentSlotCommand From(
        Guid applicationId, int kind, ParentSelectAppointmentSlotRequest body) =>
        new(applicationId, (SlotKind)kind, body);
}

public sealed class SelectParentAppointmentSlotCommandHandler(
    ICurrentUser currentUser,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<SelectParentAppointmentSlotCommandHandler> logger)
    : IRequestHandler<SelectParentAppointmentSlotCommand, Result<AdmissionAppointmentDto>>
{
    public Task<Result<AdmissionAppointmentDto>> Handle(
        SelectParentAppointmentSlotCommand request, CancellationToken ct) =>
        ParentAppointmentCommandSupport.ExecuteAsync(currentUser, repository, localizer,
            request.ApplicationId, request.Kind, ParentAppointmentActionKind.SelectSlot,
            request.Body.SlotId, request.Body.RowVersion, request.Body.IdempotencyKey, null, ct);
}
