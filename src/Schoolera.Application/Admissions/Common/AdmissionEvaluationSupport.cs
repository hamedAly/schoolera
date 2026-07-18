using Microsoft.Extensions.Localization;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public static class AdmissionEvaluationSupport
{
    public static EvaluationTemplateDto MapTemplate(SchoolAdmissionEvaluationTemplate template) =>
        new(template.Id, template.SchoolId, template.NameAr, template.NameEn, template.Kind,
            template.SchoolBranchId, template.EducationalStageId, template.GradeId,
            template.AcademicYearId, template.PublicationStatus, template.IsActive, template.Version,
            template.PublishedAtUtc,
            template.Criteria.OrderBy(x => x.SortOrder).Select(x => new EvaluationCriterionDto(
                x.Id, x.Type, x.LabelAr, x.LabelEn, x.HelpTextAr, x.HelpTextEn, x.IsRequired,
                x.SortOrder, x.ShortTextMaxLength,
                x.Options.OrderBy(o => o.SortOrder).Select(o => new EvaluationCriterionOptionDto(
                    o.Id, o.Value, o.LabelAr, o.LabelEn, o.SortOrder)).ToArray())).ToArray(),
            template.RowVersion);

    public static Result<T> RequireTemplateManager<T>(
        SchoolPortalAccessContext access, IStringLocalizer<SchoolPortalMessages> localizer)
    {
        if (!access.IsEditable ||
            (!access.IsOwner && access.MembershipRole != SchoolTeamRole.SchoolAdmin))
            return Result<T>.Failure([localizer["AccessDenied"]], [SchoolPortalErrorCodes.AccessDenied]);
        return Result<T>.Success(default!);
    }

    public static async Task<Result<AdmissionEvaluationResultDto>> ExecuteJourneyAsync(
        Guid schoolId,
        Guid applicationId,
        SlotKind kind,
        EvaluationJourneyAction action,
        byte[] rowVersion,
        string idempotencyKey,
        SaveEvaluationDraftRequest? draft,
        string? correctionReason,
        ISchoolPortalAccess portalAccess,
        IAdmissionApplicationRepository applications,
        IAdmissionEvaluationRepository evaluations,
        IStringLocalizer<AdmissionMessages> localizer,
        CancellationToken ct)
    {
        var accessResult = await portalAccess.ResolveAsync(schoolId, ct);
        if (!accessResult.Succeeded || accessResult.Data is null)
            return Result<AdmissionEvaluationResultDto>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        var access = accessResult.Data;
        if (!access.IsEditable || !access.HasPermission(SchoolPortalPermission.ManageApplicationReview))
            return Result<AdmissionEvaluationResultDto>.Failure(
                [localizer["EvaluationNotFound"]], [AdmissionErrorCodes.EvaluationNotFound]);
        if (action == EvaluationJourneyAction.BeginCorrection &&
            !access.IsOwner && access.MembershipRole != SchoolTeamRole.SchoolAdmin)
            return Result<AdmissionEvaluationResultDto>.Failure(
                [localizer["EvaluationNotFound"]], [AdmissionErrorCodes.EvaluationNotFound]);
        var application = await applications.GetForSchoolAsync(schoolId, applicationId, ct);
        if (application is null || !access.CanAccessBranch(application.SchoolBranchId))
            return Result<AdmissionEvaluationResultDto>.Failure(
                [localizer["EvaluationNotFound"]], [AdmissionErrorCodes.EvaluationNotFound]);

        var outcome = await evaluations.ExecuteJourneyAsync(new(
            schoolId, applicationId, kind, access.UserId, action, rowVersion,
            idempotencyKey, draft, correctionReason), ct);
        if (outcome.Result is EvaluationJourneyResult.Succeeded or EvaluationJourneyResult.Existing &&
            outcome.Data is not null)
            return Result<AdmissionEvaluationResultDto>.Success(outcome.Data);
        return Failure(localizer, outcome.Result);
    }

    public static Result<AdmissionEvaluationResultDto> Failure(
        IStringLocalizer<AdmissionMessages> localizer, EvaluationJourneyResult result)
    {
        var (resource, code) = result switch
        {
            EvaluationJourneyResult.NotFound => ("EvaluationNotFound", AdmissionErrorCodes.EvaluationNotFound),
            EvaluationJourneyResult.InvalidTransition => ("EvaluationInvalidTransition", AdmissionErrorCodes.EvaluationInvalidTransition),
            EvaluationJourneyResult.MissingTemplate => ("EvaluationMissingTemplate", AdmissionErrorCodes.EvaluationMissingTemplate),
            EvaluationJourneyResult.InvalidAttendance => ("EvaluationInvalidAttendance", AdmissionErrorCodes.EvaluationInvalidAttendance),
            EvaluationJourneyResult.InvalidAnswers => ("EvaluationInvalidAnswers", AdmissionErrorCodes.EvaluationInvalidAnswers),
            EvaluationJourneyResult.InvalidRecommendation => ("EvaluationInvalidRecommendation", AdmissionErrorCodes.EvaluationInvalidRecommendation),
            EvaluationJourneyResult.TooEarly => ("EvaluationTooEarly", AdmissionErrorCodes.EvaluationTooEarly),
            EvaluationJourneyResult.IdempotencyConflict => ("EvaluationIdempotencyConflict", AdmissionErrorCodes.EvaluationIdempotencyConflict),
            EvaluationJourneyResult.CorrectionBlocked => ("EvaluationCorrectionBlocked", AdmissionErrorCodes.EvaluationCorrectionBlocked),
            _ => ("EvaluationConcurrencyConflict", AdmissionErrorCodes.EvaluationConcurrencyConflict),
        };
        return Result<AdmissionEvaluationResultDto>.Failure([localizer[resource]], [code]);
    }

    public static IReadOnlyList<SchoolAdmissionEvaluationCriterion> BuildCriteria(
        Guid templateId, IReadOnlyList<EvaluationCriterionRequest> criteria) =>
        criteria.OrderBy(x => x.SortOrder).Select(x => new SchoolAdmissionEvaluationCriterion(
            templateId, x.Type, x.LabelAr, x.LabelEn, x.HelpTextAr, x.HelpTextEn,
            x.IsRequired, x.SortOrder, x.ShortTextMaxLength,
            x.Options.Select(o => (o.Value, o.LabelAr, o.LabelEn)))).ToArray();
}
