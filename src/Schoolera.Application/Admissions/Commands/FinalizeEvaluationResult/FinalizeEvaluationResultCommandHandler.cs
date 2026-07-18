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

namespace Schoolera.Application.Admissions.Commands.FinalizeEvaluationResult;

public sealed record FinalizeEvaluationResultCommand(
    Guid SchoolId, Guid ApplicationId, SlotKind Kind, FinalizeEvaluationResultRequest Body)
    : IRequest<Result<AdmissionEvaluationResultDto>>
{
    public static FinalizeEvaluationResultCommand From(
        Guid schoolId, Guid applicationId, int kind, FinalizeEvaluationResultRequest body) =>
        new(schoolId, applicationId, (SlotKind)kind, body);
}

public sealed class FinalizeEvaluationResultCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository applications,
    IAdmissionEvaluationRepository evaluations,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<FinalizeEvaluationResultCommandHandler> logger)
    : IRequestHandler<FinalizeEvaluationResultCommand, Result<AdmissionEvaluationResultDto>>
{
    public Task<Result<AdmissionEvaluationResultDto>> Handle(
        FinalizeEvaluationResultCommand request, CancellationToken ct) =>
        AdmissionEvaluationSupport.ExecuteJourneyAsync(
            request.SchoolId, request.ApplicationId, request.Kind, EvaluationJourneyAction.Finalize,
            request.Body.ResultRowVersion, request.Body.IdempotencyKey, null, null,
            portalAccess, applications, evaluations, localizer, ct);
}
