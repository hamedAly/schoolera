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

namespace Schoolera.Application.Admissions.Commands.StartEvaluationSession;

public sealed record StartEvaluationSessionCommand(
    Guid SchoolId, Guid ApplicationId, SlotKind Kind, StartEvaluationSessionRequest Body)
    : IRequest<Result<AdmissionEvaluationResultDto>>
{
    public static StartEvaluationSessionCommand From(
        Guid schoolId, Guid applicationId, int kind, StartEvaluationSessionRequest body) =>
        new(schoolId, applicationId, (SlotKind)kind, body);
}

public sealed class StartEvaluationSessionCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository applications,
    IAdmissionEvaluationRepository evaluations,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<StartEvaluationSessionCommandHandler> logger)
    : IRequestHandler<StartEvaluationSessionCommand, Result<AdmissionEvaluationResultDto>>
{
    public Task<Result<AdmissionEvaluationResultDto>> Handle(
        StartEvaluationSessionCommand request, CancellationToken ct) =>
        AdmissionEvaluationSupport.ExecuteJourneyAsync(
            request.SchoolId, request.ApplicationId, request.Kind, EvaluationJourneyAction.Start,
            request.Body.AppointmentRowVersion, request.Body.IdempotencyKey, null, null,
            portalAccess, applications, evaluations, localizer, ct);
}
