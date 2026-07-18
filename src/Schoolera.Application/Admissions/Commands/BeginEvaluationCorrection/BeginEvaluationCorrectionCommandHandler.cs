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

namespace Schoolera.Application.Admissions.Commands.BeginEvaluationCorrection;

public sealed record BeginEvaluationCorrectionCommand(
    Guid SchoolId, Guid ApplicationId, SlotKind Kind, BeginEvaluationCorrectionRequest Body)
    : IRequest<Result<AdmissionEvaluationResultDto>>
{
    public static BeginEvaluationCorrectionCommand From(
        Guid schoolId, Guid applicationId, int kind, BeginEvaluationCorrectionRequest body) =>
        new(schoolId, applicationId, (SlotKind)kind, body);
}

public sealed class BeginEvaluationCorrectionCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository applications,
    IAdmissionEvaluationRepository evaluations,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<BeginEvaluationCorrectionCommandHandler> logger)
    : IRequestHandler<BeginEvaluationCorrectionCommand, Result<AdmissionEvaluationResultDto>>
{
    public Task<Result<AdmissionEvaluationResultDto>> Handle(
        BeginEvaluationCorrectionCommand request, CancellationToken ct) =>
        AdmissionEvaluationSupport.ExecuteJourneyAsync(
            request.SchoolId, request.ApplicationId, request.Kind,
            EvaluationJourneyAction.BeginCorrection, request.Body.ResultRowVersion,
            request.Body.IdempotencyKey, null, request.Body.CorrectionReason,
            portalAccess, applications, evaluations, localizer, ct);
}
