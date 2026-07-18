using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed class AdmissionChildAgeEligibilitySnapshotService(
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IAdmissionApplicationRepository admissionRepository,
    IChildAgeEligibilityEvaluator evaluator,
    IChildProfileRepository childProfileRepository,
    ILogger<AdmissionChildAgeEligibilitySnapshotService> logger)
    : IAdmissionChildAgeEligibilitySnapshotService
{
    public async Task EnsureSnapshotAsync(
        AdmissionApplication application,
        Guid actorUserId,
        bool forceReplace = false,
        CancellationToken cancellationToken = default)
    {
        var hasSnapshot = application.AgeEligibilitySnapshot is not null ||
            await HasSnapshotAsync(application.Id, cancellationToken);

        if (!forceReplace && hasSnapshot)
        {
            return;
        }

        var hadApprovedException = false;
        if (forceReplace && hasSnapshot)
        {
            var existing = application.AgeEligibilitySnapshot
                ?? await ruleRepository.GetApplicationSnapshotAsync(application.Id, cancellationToken);

            if (existing is not null)
            {
                hadApprovedException = existing.ManualExceptionIsApproved;
                if (hadApprovedException)
                {
                    existing.ClearManualException();
                    admissionRepository.AddHistory(
                        new AdmissionApplicationHistory(
                            application.Id,
                            fromStatus: application.Status,
                            toStatus: application.Status,
                            action: AdmissionHistoryActions.AgeEligibilityExceptionInvalidated,
                            actorUserId: actorUserId,
                            actorRole: SchooleraRoles.Parent,
                            parentVisible: true,
                            parentVisibleNote: null,
                            internalNote: "scopeChange"));
                }

                await ruleRepository.RemoveApplicationSnapshotAsync(existing, cancellationToken);
                application.ClearAgeEligibilitySnapshot();
            }
        }

        var birthDate = await ResolveBirthDateAsync(application, cancellationToken);
        var evaluation = await evaluator.EvaluateAsync(
            application.SchoolId,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId,
            birthDate,
            cancellationToken);

        if (evaluation.ResultCode == ChildAgeEligibilityResultCode.RuleNotConfigured ||
            evaluation.RuleId is null ||
            evaluation.ReferenceDate is null)
        {
            logger.LogInformation(
                "No applicable published age eligibility rule for application {ApplicationId}.",
                application.Id);
            return;
        }

        var published = await ruleRepository.ListPublishedActiveAsync(
            application.SchoolId,
            cancellationToken);
        var rule = published.FirstOrDefault(item => item.Id == evaluation.RuleId.Value);
        if (rule is null)
        {
            return;
        }

        var snapshot = AdmissionApplicationChildAgeEligibilitySnapshot.FromRule(
            application.Id,
            rule,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId,
            evaluation.ReferenceDate.Value,
            evaluation.CalculatedAgeCompletedMonths,
            evaluation.ResultCode,
            birthDate);

        application.AddAgeEligibilitySnapshot(snapshot);

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: application.Status,
                toStatus: application.Status,
                action: forceReplace && hasSnapshot
                    ? AdmissionHistoryActions.AgeEligibilitySnapshotReplaced
                    : AdmissionHistoryActions.AgeEligibilitySnapshotCreated,
                actorUserId: actorUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote:
                    $"rule={rule.Id:N};version={rule.RuleVersion};result={evaluation.ResultCode}" +
                    (hadApprovedException ? ";exceptionInvalidated" : string.Empty)));

        logger.LogInformation(
            "{Action} age eligibility snapshot for application {ApplicationId} from rule {RuleId} v{Version}.",
            forceReplace && hasSnapshot ? "Replaced" : "Created",
            application.Id,
            rule.Id,
            rule.RuleVersion);
    }

    public Task<bool> HasSnapshotAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        ruleRepository.HasApplicationSnapshotAsync(applicationId, cancellationToken);

    public async Task<ChildAgeEligibilityEvaluation> RecalculateForSubmitAsync(
        AdmissionApplication application,
        DateOnly? birthDate,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        birthDate ??= await ResolveBirthDateAsync(application, cancellationToken);

        var evaluation = await evaluator.EvaluateAsync(
            application.SchoolId,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId,
            birthDate,
            cancellationToken);

        var existing = application.AgeEligibilitySnapshot
            ?? await ruleRepository.GetApplicationSnapshotAsync(application.Id, cancellationToken);

        var hadApprovedException = existing is { ManualExceptionIsApproved: true };

        if (!evaluation.CanContinue &&
            evaluation.ResultCode is ChildAgeEligibilityResultCode.NotEligibleBelowMinimum
                or ChildAgeEligibilityResultCode.NotEligibleAboveMaximum &&
            hadApprovedException &&
            existing!.ManualExceptionAllowedAtEvaluation &&
            evaluation.ManualExceptionAllowed)
        {
            evaluation = evaluation with
            {
                ResultCode = ChildAgeEligibilityResultCode.ManualExceptionApproved,
                CanContinue = true,
            };
        }

        if (evaluation.ResultCode == ChildAgeEligibilityResultCode.RuleNotConfigured)
        {
            if (existing is not null)
            {
                await ruleRepository.RemoveApplicationSnapshotAsync(existing, cancellationToken);
                application.ClearAgeEligibilitySnapshot();
            }

            return evaluation;
        }

        if (evaluation.RuleId is null || evaluation.ReferenceDate is null)
        {
            return evaluation;
        }

        var published = await ruleRepository.ListPublishedActiveAsync(
            application.SchoolId,
            cancellationToken);
        var rule = published.FirstOrDefault(item => item.Id == evaluation.RuleId.Value);
        if (rule is null)
        {
            return evaluation;
        }

        if (existing is null)
        {
            var created = AdmissionApplicationChildAgeEligibilitySnapshot.FromRule(
                application.Id,
                rule,
                application.SchoolBranchId,
                application.EducationalStageId,
                application.GradeId,
                application.AcademicYearId,
                evaluation.ReferenceDate.Value,
                evaluation.CalculatedAgeCompletedMonths,
                evaluation.ResultCode,
                birthDate);

            application.AddAgeEligibilitySnapshot(created);
            admissionRepository.AddHistory(
                new AdmissionApplicationHistory(
                    application.Id,
                    application.Status,
                    application.Status,
                    AdmissionHistoryActions.AgeEligibilitySnapshotCreated,
                    actorUserId,
                    SchooleraRoles.Parent,
                    parentVisible: true,
                    parentVisibleNote: null,
                    internalNote: $"rule={rule.Id:N};version={rule.RuleVersion};result={evaluation.ResultCode}"));
            return evaluation;
        }

        var preserveException = evaluation.ResultCode == ChildAgeEligibilityResultCode.ManualExceptionApproved &&
            hadApprovedException;
        var priorApprover = existing.ManualExceptionApprovedByUserId;
        var priorReason = existing.ManualExceptionReasonCode;
        var priorNote = existing.ManualExceptionReasonNote;

        existing.Recalculate(
            rule.Id,
            rule.RuleVersion,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId,
            rule.MinAgeCompletedMonths,
            rule.MaxAgeCompletedMonths,
            evaluation.ReferenceDate.Value,
            rule.ReferenceDateMode,
            evaluation.CalculatedAgeCompletedMonths,
            preserveException
                ? ChildAgeEligibilityResultCode.ManualExceptionApproved
                : evaluation.ResultCode,
            birthDate,
            rule.ManualExceptionAllowed);

        if (preserveException)
        {
            existing.ApplyManualException(
                priorApprover ?? actorUserId,
                priorReason ?? ChildAgeEligibilityExceptionReasonCode.SchoolReviewApproved,
                priorNote);
        }
        else if (hadApprovedException)
        {
            existing.ClearManualException();
        }

        if (application.AgeEligibilitySnapshot is null)
        {
            application.ReplaceAgeEligibilitySnapshot(existing);
        }

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.AgeEligibilityRecalculated,
                actorUserId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: $"rule={rule.Id:N};version={rule.RuleVersion};result={evaluation.ResultCode}"));

        return evaluation;
    }

    private async Task<DateOnly?> ResolveBirthDateAsync(
        AdmissionApplication application,
        CancellationToken cancellationToken)
    {
        if (application.ChildProfile is not null)
        {
            return application.ChildProfile.BirthDate;
        }

        var child = await childProfileRepository.GetOwnedAsync(
            application.ParentUserId,
            application.ChildProfileId,
            cancellationToken);
        return child?.BirthDate;
    }
}
