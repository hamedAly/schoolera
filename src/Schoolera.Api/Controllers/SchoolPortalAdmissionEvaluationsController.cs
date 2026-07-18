using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Commands.BeginEvaluationCorrection;
using Schoolera.Application.Admissions.Commands.FinalizeEvaluationResult;
using Schoolera.Application.Admissions.Commands.RecordEvaluationNoShow;
using Schoolera.Application.Admissions.Commands.SaveEvaluationDraft;
using Schoolera.Application.Admissions.Commands.StartEvaluationSession;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Admissions.Queries.GetAdmissionEvaluationResult;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/applications/{applicationId:guid}/evaluations/{kind:int}")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalAdmissionEvaluationsController(
    ISender mediator, ILogger<SchoolPortalAdmissionEvaluationsController> logger)
    : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<AdmissionEvaluationSessionContextDto>>> Get(
        Guid schoolId, Guid applicationId, int kind, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            GetAdmissionEvaluationResultQuery.From(schoolId, applicationId, kind), ct));

    [HttpPost("start")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionEvaluationResultDto>>> Start(
        Guid schoolId, Guid applicationId, int kind,
        [FromBody] StartEvaluationSessionRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            StartEvaluationSessionCommand.From(schoolId, applicationId, kind, body), ct));

    [HttpPost("draft")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionEvaluationResultDto>>> SaveDraft(
        Guid schoolId, Guid applicationId, int kind,
        [FromBody] SaveEvaluationDraftRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            SaveEvaluationDraftCommand.From(schoolId, applicationId, kind, body), ct));

    [HttpPost("finalize")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionEvaluationResultDto>>> FinalizeResult(
        Guid schoolId, Guid applicationId, int kind,
        [FromBody] FinalizeEvaluationResultRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            FinalizeEvaluationResultCommand.From(schoolId, applicationId, kind, body), ct));

    [HttpPost("no-show")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionEvaluationResultDto>>> NoShow(
        Guid schoolId, Guid applicationId, int kind,
        [FromBody] RecordEvaluationNoShowRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            RecordEvaluationNoShowCommand.From(schoolId, applicationId, kind, body), ct));

    [HttpPost("corrections")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AdmissionEvaluationResultDto>>> BeginCorrection(
        Guid schoolId, Guid applicationId, int kind,
        [FromBody] BeginEvaluationCorrectionRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            BeginEvaluationCorrectionCommand.From(schoolId, applicationId, kind, body), ct));
}
