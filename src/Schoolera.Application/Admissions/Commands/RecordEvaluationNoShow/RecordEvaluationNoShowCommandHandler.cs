using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.RecordEvaluationNoShow;

public sealed record RecordEvaluationNoShowCommand(
    Guid SchoolId, Guid ApplicationId, SlotKind Kind, RecordEvaluationNoShowRequest Body)
    : IRequest<Result<AdmissionEvaluationResultDto>>
{
    public static RecordEvaluationNoShowCommand From(
        Guid schoolId, Guid applicationId, int kind, RecordEvaluationNoShowRequest body) =>
        new(schoolId, applicationId, (SlotKind)kind, body);
}

public sealed class RecordEvaluationNoShowCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository applications,
    IAdmissionEvaluationRepository evaluations,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<RecordEvaluationNoShowCommandHandler> logger)
    : IRequestHandler<RecordEvaluationNoShowCommand, Result<AdmissionEvaluationResultDto>>
{
    public Task<Result<AdmissionEvaluationResultDto>> Handle(
        RecordEvaluationNoShowCommand request, CancellationToken ct) =>
        AdmissionEvaluationSupport.ExecuteJourneyAsync(
            request.SchoolId, request.ApplicationId, request.Kind, EvaluationJourneyAction.NoShow,
            request.Body.AppointmentRowVersion, request.Body.IdempotencyKey, null, null,
            portalAccess, applications, evaluations, localizer, ct);
}
