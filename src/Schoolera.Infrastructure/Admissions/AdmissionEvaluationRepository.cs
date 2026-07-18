using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;
using Schoolera.Application.Meetings;

namespace Schoolera.Infrastructure.Admissions;

public sealed class AdmissionEvaluationRepository(
    SchooleraDbContext db,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IMeetingSessionService meetingSessions)
    : IAdmissionEvaluationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<SchoolAdmissionEvaluationTemplate>> ListTemplatesAsync(
        Guid schoolId, EvaluationTemplateListQuery query, CancellationToken ct = default)
    {
        var source = db.SchoolAdmissionEvaluationTemplates.AsNoTracking()
            .Include(x => x.Criteria).ThenInclude(x => x.Options)
            .Where(x => x.SchoolId == schoolId);
        if (query.BranchId.HasValue) source = source.Where(x => x.SchoolBranchId == query.BranchId);
        if (query.StageId.HasValue) source = source.Where(x => x.EducationalStageId == query.StageId);
        if (query.GradeId.HasValue) source = source.Where(x => x.GradeId == query.GradeId);
        if (query.AcademicYearId.HasValue) source = source.Where(x => x.AcademicYearId == query.AcademicYearId);
        if (query.Kind.HasValue) source = source.Where(x => x.Kind == query.Kind);
        if (query.PublicationStatus.HasValue) source = source.Where(x => x.PublicationStatus == query.PublicationStatus);
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive);
        return await source.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
    }

    public Task<SchoolAdmissionEvaluationTemplate?> GetTemplateAsync(
        Guid schoolId, Guid templateId, bool tracking, CancellationToken ct = default)
    {
        var source = db.SchoolAdmissionEvaluationTemplates
            .Include(x => x.Criteria).ThenInclude(x => x.Options)
            .Where(x => x.SchoolId == schoolId && x.Id == templateId);
        return (tracking ? source : source.AsNoTracking()).SingleOrDefaultAsync(ct);
    }

    public Task AddTemplateAsync(
        SchoolAdmissionEvaluationTemplate template, CancellationToken ct = default) =>
        db.SchoolAdmissionEvaluationTemplates.AddAsync(template, ct).AsTask();

    public Task AddTemplateAuditAsync(
        SchoolAdmissionEvaluationTemplateAudit audit, CancellationToken ct = default) =>
        db.SchoolAdmissionEvaluationTemplateAudits.AddAsync(audit, ct).AsTask();

    public async Task<IReadOnlyList<SchoolAdmissionEvaluationTemplateAudit>> ListTemplateAuditAsync(
        Guid schoolId, Guid templateId, CancellationToken ct = default) =>
        await db.SchoolAdmissionEvaluationTemplateAudits.AsNoTracking()
            .Where(x => x.SchoolId == schoolId &&
                x.SchoolAdmissionEvaluationTemplateId == templateId)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);

    public Task<SchoolAdmissionEvaluationTemplateAudit?> GetTemplateAuditByKeyAsync(
        Guid schoolId, string idempotencyKey, CancellationToken ct = default) =>
        db.SchoolAdmissionEvaluationTemplateAudits.AsNoTracking().SingleOrDefaultAsync(x =>
            x.SchoolId == schoolId &&
            x.IdempotencyKey == idempotencyKey, ct);

    public async Task<bool> ScopeIsValidAsync(
        Guid schoolId, Guid? branchId, Guid? stageId, Guid? gradeId, Guid? yearId,
        CancellationToken ct = default)
    {
        if (AdmissionScope.ComputeSpecificityScore(branchId, stageId, gradeId, yearId) == 0) return false;
        if (branchId.HasValue && !await db.SchoolBranches.AsNoTracking()
                .AnyAsync(x => x.Id == branchId && x.SchoolId == schoolId && x.IsActive, ct)) return false;
        if (stageId.HasValue && !await db.EducationalStages.AsNoTracking()
                .AnyAsync(x => x.Id == stageId, ct)) return false;
        if (gradeId.HasValue && !await db.Grades.AsNoTracking()
                .AnyAsync(x => x.Id == gradeId && (!stageId.HasValue || x.EducationalStageId == stageId), ct)) return false;
        return !yearId.HasValue || await db.AcademicYears.AsNoTracking()
            .AnyAsync(x => x.Id == yearId && x.IsActive, ct);
    }

    public Task<bool> HasPublishedConflictAsync(
        Guid schoolId, string scopeKey, EvaluationTemplateKind kind, Guid? excludeId,
        CancellationToken ct = default) =>
        db.SchoolAdmissionEvaluationTemplates.AsNoTracking().AnyAsync(x =>
            x.SchoolId == schoolId && x.ScopeKey == scopeKey && x.IsActive &&
            x.PublicationStatus == EvaluationTemplatePublicationStatus.Published &&
            (!excludeId.HasValue || x.Id != excludeId) &&
            (x.Kind == kind || x.Kind == EvaluationTemplateKind.InterviewAndAssessment ||
             kind == EvaluationTemplateKind.InterviewAndAssessment), ct);

    public async Task<AdmissionEvaluationSessionContextDto?> GetContextAsync(
        Guid schoolId, Guid applicationId, SlotKind kind, Guid evaluatorUserId,
        CancellationToken ct = default)
    {
        var application = await LoadApplicationAsync(schoolId, applicationId, false, ct);
        if (application is null) return null;
        var appointment = AppointmentState.For(application, kind);
        if (appointment is null) return null;
        var result = await db.AdmissionEvaluationResults.AsNoTracking()
            .Include(x => x.Versions)
            .SingleOrDefaultAsync(x => x.AppointmentId == appointment.Id && x.Kind == kind, ct);
        var resultDto = result is null ? null : Map(result, application, appointment);
        var resolvedTemplate = resultDto?.TemplateSnapshot;
        if (resolvedTemplate is null &&
            appointment.Lifecycle == AdmissionAppointmentLifecycle.Confirmed)
        {
            var template = await ResolveTemplateAsync(application, kind, ct);
            resolvedTemplate = template is null ? null : MapTemplate(template);
        }
        var capabilities = resultDto?.Capabilities ??
            BuildCapabilities(null, application, appointment, kind);
        if (result is null && resolvedTemplate is null)
            capabilities = capabilities with { CanStartSession = false };
        return new(appointment.Id, kind, appointment.Lifecycle,
            capabilities,
            resolvedTemplate,
            resultDto);
    }

    public Task<bool> HasFinalizedRequiredEvaluationAsync(
        Guid applicationId, AdmissionApplicationStatus status, CancellationToken ct = default)
    {
        var kind = status == AdmissionApplicationStatus.InterviewRequired
            ? SlotKind.Interview
            : status == AdmissionApplicationStatus.AssessmentRequired
                ? SlotKind.Assessment
                : (SlotKind?)null;
        return kind.HasValue
            ? db.AdmissionEvaluationResults.AsNoTracking().AnyAsync(x =>
                x.AdmissionApplicationId == applicationId && x.Kind == kind.Value &&
                x.State == EvaluationResultState.Finalized && x.CurrentFinalizedVersion > 0, ct)
            : Task.FromResult(true);
    }

    public async Task<EvaluationJourneyOutcome> ExecuteJourneyAsync(
        EvaluationJourneyRequest request, CancellationToken ct = default)
    {
        var outcome = new EvaluationJourneyOutcome(EvaluationJourneyResult.ConcurrencyConflict);
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                await AcquireLockAsync($"evaluation:{request.ApplicationId:N}:{(int)request.Kind}", ct);
                var application = await LoadApplicationAsync(request.SchoolId, request.ApplicationId, true, ct);
                var expectedStatus = request.Kind == SlotKind.Interview
                    ? AdmissionApplicationStatus.InterviewRequired
                    : AdmissionApplicationStatus.AssessmentRequired;
                if (application is null || application.Status != expectedStatus || application.PolicySnapshot is null)
                {
                    outcome = new(EvaluationJourneyResult.NotFound);
                    await transaction.RollbackAsync(ct);
                    return;
                }

                var appointment = AppointmentState.For(application, request.Kind);
                if (appointment is null)
                {
                    outcome = new(EvaluationJourneyResult.NotFound);
                    await transaction.RollbackAsync(ct);
                    return;
                }

                var result = await db.AdmissionEvaluationResults
                    .Include(x => x.Versions)
                    .SingleOrDefaultAsync(x => x.AppointmentId == appointment.Id && x.Kind == request.Kind, ct);
                var key = Normalize(request.IdempotencyKey, 128);
                var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(request, JsonOptions))));
                if (key is null)
                {
                    outcome = new(EvaluationJourneyResult.IdempotencyConflict);
                    await transaction.RollbackAsync(ct);
                    return;
                }
                if (result is not null)
                {
                    var existing = await db.AdmissionEvaluationHistory.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.AdmissionEvaluationResultId == result.Id &&
                            x.IdempotencyKey == key, ct);
                    if (existing is not null)
                    {
                        var sameAction = existing.RequestFingerprint == fingerprint &&
                            (existing.Action == ToHistoryAction(request.Action) ||
                            (request.Action == EvaluationJourneyAction.Finalize &&
                             existing.Action == AdmissionEvaluationHistoryAction.CorrectionFinalized));
                        outcome = sameAction
                            ? new(EvaluationJourneyResult.Existing, Map(result, application, appointment))
                            : new(EvaluationJourneyResult.IdempotencyConflict);
                        await transaction.CommitAsync(ct);
                        return;
                    }
                }

                if (!request.RowVersion.AsSpan().SequenceEqual(
                        request.Action is EvaluationJourneyAction.Start or EvaluationJourneyAction.NoShow
                            ? appointment.RowVersion
                            : result?.RowVersion ?? []))
                {
                    outcome = new(EvaluationJourneyResult.ConcurrencyConflict);
                    await transaction.RollbackAsync(ct);
                    return;
                }

                switch (request.Action)
                {
                    case EvaluationJourneyAction.Start:
                        outcome = await StartAsync(application, appointment, request, key, fingerprint, ct);
                        break;
                    case EvaluationJourneyAction.SaveDraft:
                        outcome = SaveDraft(application, appointment, result, request, key, fingerprint);
                        break;
                    case EvaluationJourneyAction.Finalize:
                        outcome = await FinalizeAsync(application, appointment, result, request, key, fingerprint, ct);
                        break;
                    case EvaluationJourneyAction.NoShow:
                        outcome = await NoShowAsync(application, appointment, request, key, fingerprint, ct);
                        break;
                    case EvaluationJourneyAction.BeginCorrection:
                        outcome = BeginCorrection(application, appointment, result, request, key, fingerprint);
                        break;
                    default:
                        outcome = new(EvaluationJourneyResult.InvalidTransition);
                        break;
                }

                if (outcome.Result != EvaluationJourneyResult.Succeeded)
                {
                    await transaction.RollbackAsync(ct);
                    return;
                }
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                var persistedResult = await db.AdmissionEvaluationResults
                    .Include(x => x.Versions)
                    .SingleAsync(x => x.AppointmentId == appointment.Id && x.Kind == request.Kind, ct);
                outcome = new(EvaluationJourneyResult.Succeeded,
                    Map(persistedResult, application, appointment));
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(ct);
                outcome = new(EvaluationJourneyResult.ConcurrencyConflict);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                outcome = new(EvaluationJourneyResult.ConcurrencyConflict);
            }
        });
        return outcome;
    }

    private async Task<EvaluationJourneyOutcome> StartAsync(
        AdmissionApplication application, AppointmentState appointment,
        EvaluationJourneyRequest request, string key, string fingerprint, CancellationToken ct)
    {
        if (appointment.Lifecycle != AdmissionAppointmentLifecycle.Confirmed)
            return new(EvaluationJourneyResult.InvalidTransition);
        var policy = application.PolicySnapshot!;
        var end = appointment.ScheduledAtUtc.AddMinutes(policy.ExpectedDurationMinutes ?? 0);
        var now = DateTimeOffset.UtcNow;
        if (now < appointment.ScheduledAtUtc.AddMinutes(-15) || now > end)
            return new(EvaluationJourneyResult.TooEarly);

        var template = await ResolveTemplateAsync(application, request.Kind, ct);
        if (template is null) return new(EvaluationJourneyResult.MissingTemplate);
        var templateDto = MapTemplate(template);
        var result = new AdmissionEvaluationResult(
            application.Id, appointment.Id, application.SchoolId, application.SchoolBranchId,
            request.Kind, template.Id, template.Version,
            JsonSerializer.Serialize(templateDto, JsonOptions), policy.Id, policy.PolicyVersion,
            JsonSerializer.Serialize(new
            {
                appointment.SlotId,
                appointment.ScheduledAtUtc,
                appointment.TimeZoneId,
                appointment.Mode,
                ExpectedDurationMinutes = policy.ExpectedDurationMinutes,
            }, JsonOptions),
            request.EvaluatorUserId, now);
        appointment.Start();
        db.AdmissionEvaluationResults.Add(result);
        db.AdmissionEvaluationHistory.Add(new(
            result.Id, application.Id, appointment.Id, request.Kind,
            AdmissionEvaluationHistoryAction.SessionStarted, null, request.EvaluatorUserId, key,
            fingerprint));
        db.AdmissionAppointmentActionHistory.Add(new(
            application.Id, appointment.Id, request.Kind, AdmissionAppointmentAction.SessionStarted,
            AdmissionAppointmentLifecycle.Confirmed, AdmissionAppointmentLifecycle.InProgress,
            appointment.SlotId, appointment.SlotId, AdmissionAppointmentActorType.School,
            request.EvaluatorUserId, null, key));
        db.AdmissionApplicationHistory.Add(new(
            application.Id, application.Status, application.Status, "EvaluationSessionStarted",
            request.EvaluatorUserId, "SchoolEvaluator", false, null, null));
        return new(EvaluationJourneyResult.Succeeded, Map(result, application, appointment));
    }

    private EvaluationJourneyOutcome SaveDraft(
        AdmissionApplication application, AppointmentState appointment,
        AdmissionEvaluationResult? result, EvaluationJourneyRequest request, string key,
        string fingerprint)
    {
        var editableLifecycle = appointment.Lifecycle == AdmissionAppointmentLifecycle.InProgress ||
            (appointment.Lifecycle == AdmissionAppointmentLifecycle.Completed &&
             result?.CorrectsVersionNumber.HasValue == true);
        if (result is null || !editableLifecycle ||
            result.State != EvaluationResultState.Draft || request.Draft is null)
            return new(EvaluationJourneyResult.InvalidTransition);
        if (!TryValidateAnswers(result.TemplateSnapshotJson, request.Draft.Answers, false, out _))
            return new(EvaluationJourneyResult.InvalidAnswers);
        result.SaveDraft(
            JsonSerializer.Serialize(request.Draft.Answers, JsonOptions),
            request.Draft.ChildAttendance, request.Draft.ParentAttendance,
            request.Draft.Recommendation, request.Draft.SuggestedParentReasonAr,
            request.Draft.SuggestedParentReasonEn, request.Draft.InternalNotes);
        db.AdmissionEvaluationHistory.Add(new(
            result.Id, application.Id, appointment.Id, request.Kind,
            AdmissionEvaluationHistoryAction.DraftSaved, null, request.EvaluatorUserId, key,
            fingerprint));
        return new(EvaluationJourneyResult.Succeeded);
    }

    private async Task<EvaluationJourneyOutcome> FinalizeAsync(
        AdmissionApplication application, AppointmentState appointment,
        AdmissionEvaluationResult? result, EvaluationJourneyRequest request, string key,
        string fingerprint, CancellationToken ct)
    {
        var isCorrection = result?.CorrectsVersionNumber.HasValue == true;
        if (result is null || result.State != EvaluationResultState.Draft ||
            (!isCorrection && appointment.Lifecycle != AdmissionAppointmentLifecycle.InProgress) ||
            (isCorrection && appointment.Lifecycle != AdmissionAppointmentLifecycle.Completed))
            return new(EvaluationJourneyResult.InvalidTransition);
        var answers = DeserializeAnswers(result.DraftAnswersJson);
        if (!TryValidateAnswers(result.TemplateSnapshotJson, answers, true, out _))
            return new(EvaluationJourneyResult.InvalidAnswers);
        if (!ValidateAttendance(application.PolicySnapshot!, result.ChildAttendance, result.ParentAttendance))
            return new(EvaluationJourneyResult.InvalidAttendance);
        if (!ValidateRecommendation(result.Recommendation,
                result.SuggestedParentReasonAr, result.SuggestedParentReasonEn))
            return new(EvaluationJourneyResult.InvalidRecommendation);

        var action = result.CurrentFinalizedVersion == 0
            ? AdmissionEvaluationHistoryAction.ResultFinalized
            : AdmissionEvaluationHistoryAction.CorrectionFinalized;
        var endedAt = DateTimeOffset.UtcNow;
        var version = result.Finalize(request.EvaluatorUserId, endedAt);
        if (!isCorrection)
        {
            appointment.Complete();
            await meetingSessions.HandleAppointmentEndedAsync(
                appointment.Id, request.Kind, request.EvaluatorUserId,
                $"evaluation:{result.Id:N}:finalized:{version.VersionNumber}", ct);
        }
        db.AdmissionEvaluationHistory.Add(new(
            result.Id, application.Id, appointment.Id, request.Kind, action,
            version.VersionNumber, request.EvaluatorUserId, key, fingerprint));
        if (!isCorrection)
            db.AdmissionAppointmentActionHistory.Add(new(
                application.Id, appointment.Id, request.Kind, AdmissionAppointmentAction.Completed,
                AdmissionAppointmentLifecycle.InProgress, AdmissionAppointmentLifecycle.Completed,
                appointment.SlotId, appointment.SlotId, AdmissionAppointmentActorType.School,
                request.EvaluatorUserId, null, key));
        db.AdmissionApplicationHistory.Add(new(
            application.Id, application.Status, application.Status,
            isCorrection ? "EvaluationResultCorrected" : "EvaluationResultFinalized",
            request.EvaluatorUserId, "SchoolEvaluator", !isCorrection, null, null));
        if (!isCorrection)
            await EnqueueParentOperationalNotificationAsync(
                application, NotificationEventType.AppointmentCompleted,
                result.Id, version.VersionNumber, ct);
        return new(EvaluationJourneyResult.Succeeded);
    }

    private async Task<EvaluationJourneyOutcome> NoShowAsync(
        AdmissionApplication application, AppointmentState appointment,
        EvaluationJourneyRequest request, string key, string fingerprint, CancellationToken ct)
    {
        if (appointment.Lifecycle != AdmissionAppointmentLifecycle.Confirmed)
            return new(EvaluationJourneyResult.InvalidTransition);
        var policy = application.PolicySnapshot!;
        if (DateTimeOffset.UtcNow < appointment.ScheduledAtUtc.AddMinutes(policy.ExpectedDurationMinutes ?? 0))
            return new(EvaluationJourneyResult.TooEarly);

        var child = policy.RequiredParticipants is InterviewAssessmentRequiredParticipants.Child or
            InterviewAssessmentRequiredParticipants.Both
            ? EvaluationAttendance.Absent : EvaluationAttendance.NotRequired;
        var parent = policy.RequiredParticipants is InterviewAssessmentRequiredParticipants.ParentOrGuardian or
            InterviewAssessmentRequiredParticipants.Both
            ? EvaluationAttendance.Absent : EvaluationAttendance.NotRequired;
        var now = DateTimeOffset.UtcNow;
        var result = new AdmissionEvaluationResult(
            application.Id, appointment.Id, application.SchoolId, application.SchoolBranchId,
            request.Kind, Guid.Empty, 0, "{}", policy.Id, policy.PolicyVersion,
            JsonSerializer.Serialize(new
            {
                appointment.SlotId,
                appointment.ScheduledAtUtc,
                appointment.TimeZoneId,
                appointment.Mode,
                ExpectedDurationMinutes = policy.ExpectedDurationMinutes,
            }, JsonOptions), request.EvaluatorUserId, appointment.ScheduledAtUtc);
        var recommendation = policy.ParentReschedulingAllowed &&
            appointment.ParentRescheduleAttemptCount < policy.MaxParentRescheduleAttempts
            ? EvaluationRecommendation.RescheduleRequired
            : EvaluationRecommendation.ManualReviewRequired;
        result.SaveDraft("[]", child, parent, recommendation, null, null, null);
        var version = result.Finalize(request.EvaluatorUserId, now);
        appointment.NoShow();
        await meetingSessions.HandleAppointmentEndedAsync(
            appointment.Id, request.Kind, request.EvaluatorUserId,
            $"evaluation:{result.Id:N}:no-show", ct);
        db.AdmissionEvaluationResults.Add(result);
        db.AdmissionEvaluationHistory.Add(new(
            result.Id, application.Id, appointment.Id, request.Kind,
            AdmissionEvaluationHistoryAction.NoShowRecorded, version.VersionNumber,
            request.EvaluatorUserId, key, fingerprint));
        db.AdmissionAppointmentActionHistory.Add(new(
            application.Id, appointment.Id, request.Kind, AdmissionAppointmentAction.NoShow,
            AdmissionAppointmentLifecycle.Confirmed, AdmissionAppointmentLifecycle.NoShow,
            appointment.SlotId, appointment.SlotId, AdmissionAppointmentActorType.School,
            request.EvaluatorUserId, null, key));
        db.AdmissionApplicationHistory.Add(new(
            application.Id, application.Status, application.Status, "EvaluationNoShowRecorded",
            request.EvaluatorUserId, "SchoolEvaluator", true, null, null));
        await EnqueueParentOperationalNotificationAsync(
            application, NotificationEventType.AppointmentNoShow, result.Id, version.VersionNumber, ct);
        return new(EvaluationJourneyResult.Succeeded, Map(result, application, appointment));
    }

    private EvaluationJourneyOutcome BeginCorrection(
        AdmissionApplication application, AppointmentState appointment,
        AdmissionEvaluationResult? result, EvaluationJourneyRequest request, string key,
        string fingerprint)
    {
        if (result is null || result.State != EvaluationResultState.Finalized ||
            appointment.Lifecycle != AdmissionAppointmentLifecycle.Completed ||
            application.Status is not (AdmissionApplicationStatus.InterviewRequired or
                AdmissionApplicationStatus.AssessmentRequired) ||
            string.IsNullOrWhiteSpace(request.CorrectionReason))
            return new(EvaluationJourneyResult.CorrectionBlocked);
        var previous = result.Versions.Single(x => x.VersionNumber == result.CurrentFinalizedVersion);
        result.BeginCorrection(previous, request.CorrectionReason);
        db.AdmissionEvaluationHistory.Add(new(
            result.Id, application.Id, appointment.Id, request.Kind,
            AdmissionEvaluationHistoryAction.CorrectionStarted, previous.VersionNumber,
            request.EvaluatorUserId, key, fingerprint));
        return new(EvaluationJourneyResult.Succeeded);
    }

    private async Task<SchoolAdmissionEvaluationTemplate?> ResolveTemplateAsync(
        AdmissionApplication application, SlotKind kind, CancellationToken ct)
    {
        var templateKind = kind == SlotKind.Interview
            ? EvaluationTemplateKind.Interview : EvaluationTemplateKind.Assessment;
        var candidates = await db.SchoolAdmissionEvaluationTemplates.AsNoTracking()
            .Include(x => x.Criteria).ThenInclude(x => x.Options)
            .Where(x => x.SchoolId == application.SchoolId && x.IsActive &&
                x.PublicationStatus == EvaluationTemplatePublicationStatus.Published &&
                (x.Kind == templateKind || x.Kind == EvaluationTemplateKind.InterviewAndAssessment))
            .ToListAsync(ct);
        return candidates
            .Where(x => AdmissionScope.MatchesScope(
                x.SchoolBranchId, x.EducationalStageId, x.GradeId, x.AcademicYearId,
                application.SchoolBranchId, application.EducationalStageId,
                application.GradeId, application.AcademicYearId))
            .OrderByDescending(x => x.SpecificityScore).ThenBy(x => x.Id)
            .FirstOrDefault();
    }

    private static bool TryValidateAnswers(
        string snapshotJson,
        IReadOnlyList<EvaluationAnswerRequest> answers,
        bool requireAll,
        out string? error)
    {
        error = null;
        EvaluationTemplateDto? snapshot;
        try { snapshot = JsonSerializer.Deserialize<EvaluationTemplateDto>(snapshotJson, JsonOptions); }
        catch (JsonException) { return false; }
        if (snapshot is null) return false;
        if (answers.GroupBy(x => x.CriterionId).Any(x => x.Count() > 1)) return false;
        var answerMap = answers.ToDictionary(x => x.CriterionId);
        foreach (var answer in answers)
        {
            var criterion = snapshot.Criteria.SingleOrDefault(x => x.Id == answer.CriterionId);
            if (criterion is null) return false;
            var valid = criterion.Type switch
            {
                EvaluationCriterionType.YesNo =>
                    answer.YesNoValue.HasValue && NoOtherValues(answer, EvaluationCriterionType.YesNo),
                EvaluationCriterionType.RatingOneToFive =>
                    answer.RatingValue is >= 1 and <= 5 &&
                    NoOtherValues(answer, EvaluationCriterionType.RatingOneToFive),
                EvaluationCriterionType.SingleChoice =>
                    !string.IsNullOrWhiteSpace(answer.SelectedOptionValue) &&
                    criterion.Options.Any(x => x.Value == answer.SelectedOptionValue) &&
                    NoOtherValues(answer, EvaluationCriterionType.SingleChoice),
                EvaluationCriterionType.ShortText =>
                    !string.IsNullOrWhiteSpace(answer.ShortTextValue) &&
                    answer.ShortTextValue.Length <= (criterion.ShortTextMaxLength ?? 500) &&
                    !answer.ShortTextValue.Contains('<') && !answer.ShortTextValue.Contains('>') &&
                    NoOtherValues(answer, EvaluationCriterionType.ShortText),
                _ => false,
            };
            if (!valid) return false;
        }
        return !requireAll || snapshot.Criteria.Where(x => x.IsRequired)
            .All(x => answerMap.ContainsKey(x.Id));
    }

    private static bool NoOtherValues(EvaluationAnswerRequest answer, EvaluationCriterionType type) =>
        (type == EvaluationCriterionType.YesNo || !answer.YesNoValue.HasValue) &&
        (type == EvaluationCriterionType.RatingOneToFive || !answer.RatingValue.HasValue) &&
        (type == EvaluationCriterionType.SingleChoice || answer.SelectedOptionValue is null) &&
        (type == EvaluationCriterionType.ShortText || answer.ShortTextValue is null);

    private static bool ValidateAttendance(
        AdmissionApplicationInterviewAssessmentPolicySnapshot policy,
        EvaluationAttendance child,
        EvaluationAttendance parent)
    {
        if (child == EvaluationAttendance.Unknown || parent == EvaluationAttendance.Unknown) return false;
        var childRequired = policy.RequiredParticipants is InterviewAssessmentRequiredParticipants.Child or
            InterviewAssessmentRequiredParticipants.Both;
        var parentRequired = policy.RequiredParticipants is InterviewAssessmentRequiredParticipants.ParentOrGuardian or
            InterviewAssessmentRequiredParticipants.Both;
        return (childRequired ? child is EvaluationAttendance.Present or EvaluationAttendance.Absent
                : child == EvaluationAttendance.NotRequired) &&
            (parentRequired ? parent is EvaluationAttendance.Present or EvaluationAttendance.Absent
                : parent == EvaluationAttendance.NotRequired);
    }

    private static bool ValidateRecommendation(
        EvaluationRecommendation recommendation, string? reasonAr, string? reasonEn)
    {
        if (!Enum.IsDefined(recommendation) || recommendation == EvaluationRecommendation.NoRecommendation)
            return false;
        if (recommendation == EvaluationRecommendation.RescheduleRequired)
            return false;
        if (recommendation is EvaluationRecommendation.RecommendReject or
            EvaluationRecommendation.RecommendWaitlist)
            return !string.IsNullOrWhiteSpace(reasonAr) && !string.IsNullOrWhiteSpace(reasonEn);
        return true;
    }

    private static IReadOnlyList<EvaluationAnswerRequest> DeserializeAnswers(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<EvaluationAnswerRequest>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static EvaluationTemplateDto MapTemplate(SchoolAdmissionEvaluationTemplate template) =>
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

    private static AdmissionEvaluationResultDto Map(
        AdmissionEvaluationResult result, AdmissionApplication application, AppointmentState appointment)
    {
        EvaluationTemplateDto? template = null;
        if (result.TemplateId != Guid.Empty)
        {
            try { template = JsonSerializer.Deserialize<EvaluationTemplateDto>(result.TemplateSnapshotJson, JsonOptions); }
            catch (JsonException) { }
        }
        var answers = DeserializeAnswers(result.DraftAnswersJson).Select(MapAnswer).ToArray();
        var versions = result.Versions.OrderByDescending(x => x.VersionNumber).Select(x =>
            new EvaluationResultVersionDto(
                x.VersionNumber, x.StartedAtUtc, x.EndedAtUtc, x.ChildAttendance,
                x.ParentAttendance, DeserializeAnswers(x.AnswersJson).Select(MapAnswer).ToArray(),
                x.Recommendation, x.SuggestedParentReasonAr, x.SuggestedParentReasonEn,
                x.InternalNotes, x.FinalizedAtUtc, x.CorrectionReason, x.PreviousVersionNumber))
            .ToArray();
        var capabilities = BuildCapabilities(result, application, appointment, result.Kind);
        return new(result.Id, result.AdmissionApplicationId, result.AppointmentId, result.Kind,
            result.State, result.StartedAtUtc, result.CurrentFinalizedVersion, template,
            result.ChildAttendance, result.ParentAttendance, answers, result.Recommendation,
            result.SuggestedParentReasonAr, result.SuggestedParentReasonEn, result.InternalNotes,
            versions, capabilities, result.RowVersion);
    }

    private static EvaluationExecutionCapabilitiesDto BuildCapabilities(
        AdmissionEvaluationResult? result,
        AdmissionApplication application,
        AppointmentState appointment,
        SlotKind kind)
    {
        var expectedStatus = kind == SlotKind.Interview
            ? AdmissionApplicationStatus.InterviewRequired : AdmissionApplicationStatus.AssessmentRequired;
        var now = DateTimeOffset.UtcNow;
        var end = appointment.ScheduledAtUtc.AddMinutes(
            application.PolicySnapshot?.ExpectedDurationMinutes ?? 0);
        var matchingWorkflow = application.Status == expectedStatus;
        var editableDraft = matchingWorkflow && result?.State == EvaluationResultState.Draft &&
            (appointment.Lifecycle == AdmissionAppointmentLifecycle.InProgress ||
             (appointment.Lifecycle == AdmissionAppointmentLifecycle.Completed &&
              result.CorrectsVersionNumber.HasValue));
        return new(
            matchingWorkflow && result is null &&
                appointment.Lifecycle == AdmissionAppointmentLifecycle.Confirmed &&
                now >= appointment.ScheduledAtUtc.AddMinutes(-15) && now <= end,
            editableDraft,
            editableDraft,
            matchingWorkflow && result is null &&
                appointment.Lifecycle == AdmissionAppointmentLifecycle.Confirmed && now >= end,
            matchingWorkflow && appointment.Lifecycle == AdmissionAppointmentLifecycle.Completed &&
                result?.State == EvaluationResultState.Finalized,
            matchingWorkflow &&
                (appointment.Lifecycle is AdmissionAppointmentLifecycle.Completed or
                    AdmissionAppointmentLifecycle.NoShow) &&
                result?.State == EvaluationResultState.Finalized,
            appointment.ScheduledAtUtc.AddMinutes(-15),
            end);
    }

    private static EvaluationAnswerDto MapAnswer(EvaluationAnswerRequest answer) =>
        new(answer.CriterionId, answer.YesNoValue, answer.RatingValue,
            answer.SelectedOptionValue, answer.ShortTextValue);

    private async Task<AdmissionApplication?> LoadApplicationAsync(
        Guid schoolId, Guid applicationId, bool tracking, CancellationToken ct)
    {
        var source = db.AdmissionApplications
            .Include(x => x.PolicySnapshot)
            .Include(x => x.InterviewAppointments)
            .Include(x => x.AssessmentAppointments)
            .Where(x => x.SchoolId == schoolId && x.Id == applicationId);
        return await (tracking ? source : source.AsNoTracking()).SingleOrDefaultAsync(ct);
    }

    private async Task EnqueueParentOperationalNotificationAsync(
        AdmissionApplication application, NotificationEventType eventType,
        Guid resultId, int version, CancellationToken ct)
    {
        var culture = await db.Users.AsNoTracking().Where(x => x.Id == application.ParentUserId)
            .Select(x => x.PreferredLanguage).SingleOrDefaultAsync(ct);
        await notificationOutboxPublisher.EnqueueAsync(new(
            application.ParentUserId, eventType,
            culture?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "ar",
            $"evaluation:{resultId:N}:version:{version}:recipient:{application.ParentUserId:N}",
            new Dictionary<string, string> { ["applicationNumber"] = application.ApplicationNumber },
            $"/parent/applications/{application.Id}", application.SchoolId, application.Id,
            [NotificationChannel.InApp], SkipPreferenceCheck: true), ct);
    }

    private Task AcquireLockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            IF @lockResult < 0 THROW 51000, 'Unable to acquire evaluation lock.', 1;
            """, ct);

    private static AdmissionEvaluationHistoryAction ToHistoryAction(EvaluationJourneyAction action) =>
        action switch
        {
            EvaluationJourneyAction.Start => AdmissionEvaluationHistoryAction.SessionStarted,
            EvaluationJourneyAction.SaveDraft => AdmissionEvaluationHistoryAction.DraftSaved,
            EvaluationJourneyAction.Finalize => AdmissionEvaluationHistoryAction.ResultFinalized,
            EvaluationJourneyAction.NoShow => AdmissionEvaluationHistoryAction.NoShowRecorded,
            EvaluationJourneyAction.BeginCorrection => AdmissionEvaluationHistoryAction.CorrectionStarted,
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized[..Math.Min(max, normalized.Length)];
    }

    private sealed class AppointmentState
    {
        private readonly AdmissionInterviewAppointment? interview;
        private readonly AdmissionAssessmentAppointment? assessment;
        private AppointmentState(AdmissionInterviewAppointment value) => interview = value;
        private AppointmentState(AdmissionAssessmentAppointment value) => assessment = value;

        public static AppointmentState? For(AdmissionApplication application, SlotKind kind)
        {
            if (kind == SlotKind.Interview)
            {
                var value = application.InterviewAppointments.OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefault();
                return value is null ? null : new(value);
            }
            var assessment = application.AssessmentAppointments.OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();
            return assessment is null ? null : new(assessment);
        }

        public Guid Id => interview?.Id ?? assessment!.Id;
        public Guid? SlotId => interview?.InterviewAssessmentSlotId ?? assessment!.InterviewAssessmentSlotId;
        public DateTimeOffset ScheduledAtUtc => interview?.ScheduledAtUtc ?? assessment!.ScheduledAtUtc;
        public string TimeZoneId => interview?.TimeZoneId ?? assessment!.TimeZoneId;
        public AdmissionAppointmentMode Mode => interview?.Mode ?? assessment!.Mode;
        public AdmissionAppointmentLifecycle Lifecycle => interview?.Lifecycle ?? assessment!.Lifecycle;
        public byte[] RowVersion => interview?.RowVersion ?? assessment!.RowVersion;
        public int ParentRescheduleAttemptCount =>
            interview?.ParentRescheduleAttemptCount ?? assessment!.ParentRescheduleAttemptCount;
        public void Start()
        {
            if (interview is not null) interview.StartSession(); else assessment!.StartSession();
        }
        public void Complete()
        {
            if (interview is not null) interview.Complete(null); else assessment!.Complete(null);
        }
        public void NoShow()
        {
            if (interview is not null) interview.MarkNoShow(); else assessment!.MarkNoShow();
        }
    }
}
