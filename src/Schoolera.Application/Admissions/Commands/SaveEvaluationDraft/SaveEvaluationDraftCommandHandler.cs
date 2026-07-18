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

namespace Schoolera.Application.Admissions.Commands.SaveEvaluationDraft;

public sealed record SaveEvaluationDraftCommand(
    Guid SchoolId, Guid ApplicationId, SlotKind Kind, SaveEvaluationDraftRequest Body)
    : IRequest<Result<AdmissionEvaluationResultDto>>
{
    public static SaveEvaluationDraftCommand From(
        Guid schoolId, Guid applicationId, int kind, SaveEvaluationDraftRequest body) =>
        new(schoolId, applicationId, (SlotKind)kind, body);
}

public sealed class SaveEvaluationDraftCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository applications,
    IAdmissionEvaluationRepository evaluations,
    IStringLocalizer<AdmissionMessages> localizer,
    IUnitOfWork unitOfWork,
    ILogger<SaveEvaluationDraftCommandHandler> logger)
    : IRequestHandler<SaveEvaluationDraftCommand, Result<AdmissionEvaluationResultDto>>
{
    public Task<Result<AdmissionEvaluationResultDto>> Handle(
        SaveEvaluationDraftCommand request, CancellationToken ct) =>
        AdmissionEvaluationSupport.ExecuteJourneyAsync(
            request.SchoolId, request.ApplicationId, request.Kind, EvaluationJourneyAction.SaveDraft,
            request.Body.RowVersion, request.Body.IdempotencyKey, request.Body, null,
            portalAccess, applications, evaluations, localizer, ct);
}
