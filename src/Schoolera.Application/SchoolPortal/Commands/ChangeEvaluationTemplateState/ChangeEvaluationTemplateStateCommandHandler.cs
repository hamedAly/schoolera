using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;

namespace Schoolera.Application.SchoolPortal.Commands.ChangeEvaluationTemplateState;

public enum EvaluationTemplateStateAction
{
    Publish = 1,
    Unpublish = 2,
    Deactivate = 3,
    Clone = 4,
}

public sealed record ChangeEvaluationTemplateStateCommand(
    Guid SchoolId,
    Guid TemplateId,
    EvaluationTemplateStateAction Action,
    EvaluationTemplateActionRequest Body)
    : IRequest<Result<EvaluationTemplateDto>>;

public sealed class ChangeEvaluationTemplateStateCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionEvaluationRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> portalLocalizer,
    IStringLocalizer<AdmissionMessages> admissionLocalizer,
    ILogger<ChangeEvaluationTemplateStateCommandHandler> logger)
    : IRequestHandler<ChangeEvaluationTemplateStateCommand, Result<EvaluationTemplateDto>>
{
    public async Task<Result<EvaluationTemplateDto>> Handle(
        ChangeEvaluationTemplateStateCommand request, CancellationToken ct)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, ct);
        if (!accessResult.Succeeded || accessResult.Data is null)
            return Result<EvaluationTemplateDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        var permission = AdmissionEvaluationSupport.RequireTemplateManager<EvaluationTemplateDto>(
            accessResult.Data, portalLocalizer);
        if (!permission.Succeeded) return permission;
        var template = await repository.GetTemplateAsync(
            request.SchoolId, request.TemplateId, true, ct);
        if (template is null)
            return Result<EvaluationTemplateDto>.Failure(
                [admissionLocalizer["EvaluationNotFound"]], [AdmissionErrorCodes.EvaluationNotFound]);
        var existingAction = await repository.GetTemplateAuditByKeyAsync(
            request.SchoolId, request.Body.IdempotencyKey, ct);
        if (existingAction is not null)
        {
            if (existingAction.SchoolAdmissionEvaluationTemplateId != template.Id)
                return Result<EvaluationTemplateDto>.Failure(
                    [admissionLocalizer["EvaluationIdempotencyConflict"]],
                    [AdmissionErrorCodes.EvaluationIdempotencyConflict]);
            if (request.Action == EvaluationTemplateStateAction.Clone &&
                existingAction.Action.StartsWith("Clone:", StringComparison.Ordinal) &&
                Guid.TryParse(existingAction.Action[6..], out var cloneId))
            {
                var existingClone = await repository.GetTemplateAsync(
                    request.SchoolId, cloneId, false, ct);
                if (existingClone is not null)
                    return Result<EvaluationTemplateDto>.Success(
                        AdmissionEvaluationSupport.MapTemplate(existingClone));
            }
            if (existingAction.Action == request.Action.ToString())
                return Result<EvaluationTemplateDto>.Success(
                    AdmissionEvaluationSupport.MapTemplate(template));
            return Result<EvaluationTemplateDto>.Failure(
                [admissionLocalizer["EvaluationIdempotencyConflict"]],
                [AdmissionErrorCodes.EvaluationIdempotencyConflict]);
        }
        if (!request.Body.RowVersion.AsSpan().SequenceEqual(template.RowVersion))
            return Result<EvaluationTemplateDto>.Failure(
                [admissionLocalizer["EvaluationConcurrencyConflict"]],
                [AdmissionErrorCodes.EvaluationConcurrencyConflict]);

        if (request.Action == EvaluationTemplateStateAction.Publish)
        {
            if (await repository.HasPublishedConflictAsync(
                    request.SchoolId, template.ScopeKey, template.Kind, template.Id, ct))
                return Result<EvaluationTemplateDto>.Failure(
                    [admissionLocalizer["EvaluationTemplateConflict"]],
                    [AdmissionErrorCodes.EvaluationTemplateConflict]);
            template.Publish(accessResult.Data.UserId);
        }
        else if (request.Action == EvaluationTemplateStateAction.Unpublish)
            template.Unpublish(accessResult.Data.UserId);
        else if (request.Action == EvaluationTemplateStateAction.Deactivate)
            template.Deactivate(accessResult.Data.UserId);
        else if (request.Action == EvaluationTemplateStateAction.Clone)
        {
            var clone = template.CloneAsDraft(accessResult.Data.UserId);
            await repository.AddTemplateAsync(clone, ct);
            await repository.AddTemplateAuditAsync(new(
                request.SchoolId, clone.Id, "Cloned", accessResult.Data.UserId, clone.Version), ct);
            await repository.AddTemplateAuditAsync(new(
                request.SchoolId, template.Id, $"Clone:{clone.Id}",
                accessResult.Data.UserId, template.Version, request.Body.IdempotencyKey), ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result<EvaluationTemplateDto>.Success(
                AdmissionEvaluationSupport.MapTemplate(clone));
        }
        await repository.AddTemplateAuditAsync(new(
            request.SchoolId, template.Id, request.Action.ToString(),
            accessResult.Data.UserId, template.Version, request.Body.IdempotencyKey), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<EvaluationTemplateDto>.Success(
            AdmissionEvaluationSupport.MapTemplate(template));
    }
}
