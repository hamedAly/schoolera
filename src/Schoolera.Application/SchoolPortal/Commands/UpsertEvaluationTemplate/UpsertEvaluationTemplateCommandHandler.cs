using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.UpsertEvaluationTemplate;

public sealed record UpsertEvaluationTemplateCommand(
    Guid SchoolId, Guid? TemplateId, UpsertEvaluationTemplateRequest Body)
    : IRequest<Result<EvaluationTemplateDto>>;

public sealed class UpsertEvaluationTemplateCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionEvaluationRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> portalLocalizer,
    IStringLocalizer<AdmissionMessages> admissionLocalizer,
    ILogger<UpsertEvaluationTemplateCommandHandler> logger)
    : IRequestHandler<UpsertEvaluationTemplateCommand, Result<EvaluationTemplateDto>>
{
    public async Task<Result<EvaluationTemplateDto>> Handle(
        UpsertEvaluationTemplateCommand request, CancellationToken ct)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, ct);
        if (!accessResult.Succeeded || accessResult.Data is null)
            return Result<EvaluationTemplateDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        var permission = AdmissionEvaluationSupport.RequireTemplateManager<EvaluationTemplateDto>(
            accessResult.Data, portalLocalizer);
        if (!permission.Succeeded) return permission;
        var baseAction = request.TemplateId.HasValue ? "Updated" : "Created";
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request.Body))));
        var auditAction = $"{baseAction}:{fingerprint}";
        var existingAction = await repository.GetTemplateAuditByKeyAsync(
            request.SchoolId, request.Body.IdempotencyKey, ct);
        if (existingAction is not null)
        {
            if (existingAction.Action == auditAction &&
                (!request.TemplateId.HasValue ||
                 existingAction.SchoolAdmissionEvaluationTemplateId == request.TemplateId))
            {
                var existingTemplate = await repository.GetTemplateAsync(
                    request.SchoolId,
                    existingAction.SchoolAdmissionEvaluationTemplateId, false, ct);
                if (existingTemplate is not null)
                    return Result<EvaluationTemplateDto>.Success(
                        AdmissionEvaluationSupport.MapTemplate(existingTemplate));
            }
            return Result<EvaluationTemplateDto>.Failure(
                [admissionLocalizer["EvaluationIdempotencyConflict"]],
                [AdmissionErrorCodes.EvaluationIdempotencyConflict]);
        }
        if (!await repository.ScopeIsValidAsync(
                request.SchoolId, request.Body.SchoolBranchId,
                request.Body.EducationalStageId, request.Body.GradeId,
                request.Body.AcademicYearId, ct))
            return Result<EvaluationTemplateDto>.Failure(
                [admissionLocalizer["EvaluationTemplateInvalidScope"]],
                [AdmissionErrorCodes.EvaluationTemplateInvalidScope]);

        SchoolAdmissionEvaluationTemplate template;
        if (request.TemplateId.HasValue)
        {
            var existing = await repository.GetTemplateAsync(
                request.SchoolId, request.TemplateId.Value, true, ct);
            if (existing is null)
                return Result<EvaluationTemplateDto>.Failure(
                    [admissionLocalizer["EvaluationNotFound"]],
                    [AdmissionErrorCodes.EvaluationNotFound]);
            template = existing;
            if (request.Body.RowVersion is null ||
                !request.Body.RowVersion.AsSpan().SequenceEqual(template.RowVersion))
                return Result<EvaluationTemplateDto>.Failure(
                    [admissionLocalizer["EvaluationConcurrencyConflict"]],
                    [AdmissionErrorCodes.EvaluationConcurrencyConflict]);
            template.UpdateDraft(
                request.Body.NameAr, request.Body.NameEn, request.Body.Kind,
                request.Body.SchoolBranchId, request.Body.EducationalStageId,
                request.Body.GradeId, request.Body.AcademicYearId, accessResult.Data.UserId);
        }
        else
        {
            template = new(
                request.SchoolId, request.Body.NameAr, request.Body.NameEn, request.Body.Kind,
                request.Body.SchoolBranchId, request.Body.EducationalStageId,
                request.Body.GradeId, request.Body.AcademicYearId, accessResult.Data.UserId);
            await repository.AddTemplateAsync(template, ct);
        }
        template.ReplaceCriteria(
            AdmissionEvaluationSupport.BuildCriteria(template.Id, request.Body.Criteria),
            accessResult.Data.UserId);
        await repository.AddTemplateAuditAsync(new(
            request.SchoolId, template.Id, auditAction,
            accessResult.Data.UserId, template.Version, request.Body.IdempotencyKey), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<EvaluationTemplateDto>.Success(
            AdmissionEvaluationSupport.MapTemplate(template));
    }
}
