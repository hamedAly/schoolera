using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.ChangeEvaluationTemplateState;
using Schoolera.Application.SchoolPortal.Commands.UpsertEvaluationTemplate;
using Schoolera.Application.SchoolPortal.Queries.ListEvaluationTemplates;
using Schoolera.Application.SchoolPortal.Queries.ListEvaluationTemplateAudit;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/evaluation-templates")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalEvaluationTemplatesController(
    ISender mediator, ILogger<SchoolPortalEvaluationTemplatesController> logger)
    : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<EvaluationTemplateDto>>>> List(
        Guid schoolId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? stageId,
        [FromQuery] Guid? gradeId,
        [FromQuery] Guid? academicYearId,
        [FromQuery] int? kind,
        [FromQuery] int? publicationStatus,
        [FromQuery] bool? isActive,
        CancellationToken ct) =>
        FromResult(await Mediator.Send(ListEvaluationTemplatesQuery.From(
            schoolId, branchId, stageId, gradeId, academicYearId,
            kind, publicationStatus, isActive), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<EvaluationTemplateDto>>> Create(
        Guid schoolId, [FromBody] UpsertEvaluationTemplateRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpsertEvaluationTemplateCommand(
            schoolId, null, body), ct));

    [HttpGet("{templateId:guid}/audit")]
    public async Task<ActionResult<Result<IReadOnlyList<EvaluationTemplateAuditDto>>>> Audit(
        Guid schoolId, Guid templateId, CancellationToken ct) =>
        FromResult(await Mediator.Send(
            new ListEvaluationTemplateAuditQuery(schoolId, templateId), ct));

    [HttpPut("{templateId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<EvaluationTemplateDto>>> Update(
        Guid schoolId, Guid templateId,
        [FromBody] UpsertEvaluationTemplateRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new UpsertEvaluationTemplateCommand(
            schoolId, templateId, body), ct));

    [HttpPost("{templateId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<EvaluationTemplateDto>>> Publish(
        Guid schoolId, Guid templateId, [FromBody] EvaluationTemplateActionRequest body,
        CancellationToken ct) =>
        ChangeState(schoolId, templateId, EvaluationTemplateStateAction.Publish, body, ct);

    [HttpPost("{templateId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<EvaluationTemplateDto>>> Unpublish(
        Guid schoolId, Guid templateId, [FromBody] EvaluationTemplateActionRequest body,
        CancellationToken ct) =>
        ChangeState(schoolId, templateId, EvaluationTemplateStateAction.Unpublish, body, ct);

    [HttpPost("{templateId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<EvaluationTemplateDto>>> Deactivate(
        Guid schoolId, Guid templateId, [FromBody] EvaluationTemplateActionRequest body,
        CancellationToken ct) =>
        ChangeState(schoolId, templateId, EvaluationTemplateStateAction.Deactivate, body, ct);

    [HttpPost("{templateId:guid}/clone")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<EvaluationTemplateDto>>> Clone(
        Guid schoolId, Guid templateId, [FromBody] EvaluationTemplateActionRequest body,
        CancellationToken ct) =>
        ChangeState(schoolId, templateId, EvaluationTemplateStateAction.Clone, body, ct);

    private async Task<ActionResult<Result<EvaluationTemplateDto>>> ChangeState(
        Guid schoolId, Guid templateId, EvaluationTemplateStateAction action,
        EvaluationTemplateActionRequest body, CancellationToken ct) =>
        FromResult(await Mediator.Send(new ChangeEvaluationTemplateStateCommand(
            schoolId, templateId, action, body), ct));
}
