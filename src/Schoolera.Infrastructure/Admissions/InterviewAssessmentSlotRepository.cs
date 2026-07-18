using System.Data;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Application.Meetings;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admissions;

public sealed class InterviewAssessmentSlotRepository(
    SchooleraDbContext db,
    IIntegrationSettingsValidator integrationSettingsValidator,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IMeetingSessionService meetingSessions) : IInterviewAssessmentSlotRepository
{
    public Task<InterviewAssessmentSlot?> GetAsync(Guid schoolId, Guid slotId, bool tracking,
        CancellationToken ct = default)
    {
        var q = db.InterviewAssessmentSlots.Where(x => x.SchoolId == schoolId && x.Id == slotId);
        return (tracking ? q : q.AsNoTracking()).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<InterviewAssessmentSlot>> ListAsync(
        Guid schoolId, Guid? branchId, Guid? stageId, Guid? gradeId, Guid? yearId,
        SlotKind? kind, SlotDeliveryMode? mode, SlotStatus? status, Guid? resourceId,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var q = db.InterviewAssessmentSlots.AsNoTracking().Where(x => x.SchoolId == schoolId);
        if (branchId.HasValue) q = q.Where(x => x.SchoolBranchId == branchId);
        if (stageId.HasValue) q = q.Where(x => x.EducationalStageId == stageId);
        if (gradeId.HasValue) q = q.Where(x => x.GradeId == gradeId);
        if (yearId.HasValue) q = q.Where(x => x.AcademicYearId == yearId);
        if (kind.HasValue) q = q.Where(x => x.Kind == kind);
        if (mode.HasValue) q = q.Where(x => x.DeliveryMode == mode);
        if (status.HasValue) q = q.Where(x => x.Status == status);
        if (resourceId.HasValue) q = q.Where(x => x.ResourceReferenceId == resourceId);
        if (from.HasValue) q = q.Where(x => x.EndAtUtc > from);
        if (to.HasValue) q = q.Where(x => x.StartAtUtc < to);
        return await q.OrderBy(x => x.StartAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
    }

    public Task AddAsync(InterviewAssessmentSlot slot, CancellationToken ct = default) =>
        db.InterviewAssessmentSlots.AddAsync(slot, ct).AsTask();
    public Task AddAuditAsync(InterviewAssessmentSlotAudit audit, CancellationToken ct = default) =>
        db.InterviewAssessmentSlotAudits.AddAsync(audit, ct).AsTask();
    public async Task<IReadOnlyList<InterviewAssessmentSlotAudit>> ListAuditAsync(
        Guid schoolId, Guid slotId, CancellationToken ct = default) =>
        await db.InterviewAssessmentSlotAudits.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.InterviewAssessmentSlotId == slotId)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);

    public Task<bool> HasConflictAsync(Guid schoolId, Guid branchId, SlotResourceKind? resourceKind,
        Guid? resourceId, DateTimeOffset start, DateTimeOffset end, Guid? exclude,
        CancellationToken ct = default)
    {
        if (!resourceKind.HasValue || !resourceId.HasValue)
            return Task.FromResult(false);

        return db.InterviewAssessmentSlots.AsNoTracking().AnyAsync(x =>
            x.SchoolId == schoolId && x.Status == SlotStatus.Open &&
            x.ResourceKind == resourceKind && x.ResourceReferenceId == resourceId &&
            (!exclude.HasValue || x.Id != exclude) &&
            x.StartAtUtc < end && x.EndAtUtc > start, ct);
    }

    public async Task<SlotAssignmentResult> ExecuteAtomicAssignmentAsync(
        Guid schoolId, Guid slotId, int expectedCapacity, Guid? excludeAppointmentId,
        Func<CancellationToken, Task> mutation, CancellationToken ct = default)
    {
        var result = SlotAssignmentResult.ConcurrencyConflict;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
            try
            {
                var slot = await db.InterviewAssessmentSlots
                    .FromSqlInterpolated($"""
                        SELECT * FROM [InterviewAssessmentSlots] WITH (UPDLOCK,HOLDLOCK)
                        WHERE [SchoolId] = {schoolId} AND [Id] = {slotId}
                        """)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct);
                if (slot is null || slot.Status != SlotStatus.Open ||
                    slot.Capacity != expectedCapacity)
                {
                    result = SlotAssignmentResult.Unavailable;
                    await transaction.RollbackAsync(ct);
                    return;
                }

                var interviews = db.AdmissionInterviewAppointments.Where(x =>
                    x.InterviewAssessmentSlotId == slotId &&
                    (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
                     x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
                     x.Lifecycle == AdmissionAppointmentLifecycle.InProgress) &&
                    (!excludeAppointmentId.HasValue || x.Id != excludeAppointmentId));
                var assessments = db.AdmissionAssessmentAppointments.Where(x =>
                    x.InterviewAssessmentSlotId == slotId &&
                    (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
                     x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
                     x.Lifecycle == AdmissionAppointmentLifecycle.InProgress) &&
                    (!excludeAppointmentId.HasValue || x.Id != excludeAppointmentId));
                if (await interviews.CountAsync(ct) + await assessments.CountAsync(ct) >= slot.Capacity)
                {
                    result = SlotAssignmentResult.Full;
                    await transaction.RollbackAsync(ct);
                    return;
                }

                await mutation(ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                result = SlotAssignmentResult.Succeeded;
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                result = SlotAssignmentResult.ConcurrencyConflict;
            }
        });
        return result;
    }

    public async Task<SlotAssignmentResult> ExecuteAtomicSchoolProposalAsync(
        Guid schoolId, Guid slotId, int expectedCapacity, Guid? excludeAppointmentId,
        Guid applicationId, SlotKind kind, AdmissionAppointmentAction action,
        string idempotencyKey, Func<CancellationToken, Task> mutation,
        CancellationToken ct = default)
    {
        var result = SlotAssignmentResult.ConcurrencyConflict;
        var normalizedKey = idempotencyKey.Trim();
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
            try
            {
                await AcquireApplicationLockAsync(
                    $"school-appointment-proposal:{applicationId:N}:{normalizedKey}", ct);
                var existing = await db.AdmissionAppointmentActionHistory.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AdmissionApplicationId == applicationId &&
                        x.IdempotencyKey == normalizedKey, ct);
                if (existing is not null)
                {
                    result = existing.ActorType == AdmissionAppointmentActorType.School &&
                             existing.Kind == kind &&
                             existing.Action == action &&
                             existing.NewSlotId == slotId
                        ? SlotAssignmentResult.Existing
                        : SlotAssignmentResult.IdempotencyConflict;
                    await transaction.CommitAsync(ct);
                    return;
                }

                var slot = await db.InterviewAssessmentSlots
                    .FromSqlInterpolated($"""
                        SELECT * FROM [InterviewAssessmentSlots] WITH (UPDLOCK,HOLDLOCK)
                        WHERE [SchoolId] = {schoolId} AND [Id] = {slotId}
                        """)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct);
                if (slot is null || slot.Status != SlotStatus.Open ||
                    slot.Capacity != expectedCapacity)
                {
                    result = SlotAssignmentResult.Unavailable;
                    await transaction.RollbackAsync(ct);
                    return;
                }

                if (await CountReservationsAsync(slotId, excludeAppointmentId ?? Guid.Empty, ct)
                    >= slot.Capacity)
                {
                    result = SlotAssignmentResult.Full;
                    await transaction.RollbackAsync(ct);
                    return;
                }

                await mutation(ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                result = SlotAssignmentResult.Succeeded;
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                result = SlotAssignmentResult.ConcurrencyConflict;
            }
        });
        return result;
    }

    public async Task<SlotAssignmentResult> CheckSchoolProposalIdempotencyAsync(
        Guid applicationId, SlotKind kind, AdmissionAppointmentAction action,
        Guid slotId, string idempotencyKey, CancellationToken ct = default)
    {
        var result = SlotAssignmentResult.Unavailable;
        var normalizedKey = idempotencyKey.Trim();
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
            await AcquireApplicationLockAsync(
                $"school-appointment-proposal:{applicationId:N}:{normalizedKey}", ct);
            var existing = await db.AdmissionAppointmentActionHistory.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AdmissionApplicationId == applicationId &&
                    x.IdempotencyKey == normalizedKey, ct);
            if (existing is not null)
                result = existing.ActorType == AdmissionAppointmentActorType.School &&
                         existing.Kind == kind && existing.Action == action &&
                         existing.NewSlotId == slotId
                    ? SlotAssignmentResult.Existing
                    : SlotAssignmentResult.IdempotencyConflict;
            await transaction.CommitAsync(ct);
        });
        return result;
    }

    public async Task<AtomicSlotOpenResult> ExecuteAtomicOpenAsync(
        Guid schoolId, Guid slotId, SlotResourceKind? resourceKind, Guid? resourceId,
        DateTimeOffset start, DateTimeOffset end, Func<CancellationToken, Task> mutation,
        CancellationToken ct = default)
    {
        var result = AtomicSlotOpenResult.ConcurrencyConflict;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
            try
            {
                if (resourceKind.HasValue && resourceId.HasValue)
                {
                    var lockKey = $"slot-open:{schoolId:N}:{(int)resourceKind}:{resourceId:N}";
                    await AcquireApplicationLockAsync(lockKey, ct);
                    var conflict = await db.InterviewAssessmentSlots.AsNoTracking().AnyAsync(x =>
                        x.SchoolId == schoolId && x.Id != slotId && x.Status == SlotStatus.Open &&
                        x.ResourceKind == resourceKind && x.ResourceReferenceId == resourceId &&
                        x.StartAtUtc < end && x.EndAtUtc > start, ct);
                    if (conflict)
                    {
                        result = AtomicSlotOpenResult.ResourceConflict;
                        await transaction.RollbackAsync(ct);
                        return;
                    }
                }

                await mutation(ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                result = AtomicSlotOpenResult.Succeeded;
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                result = AtomicSlotOpenResult.ConcurrencyConflict;
            }
        });
        return result;
    }

    public async Task<AtomicSlotGenerationOutcome> ExecuteAtomicGenerationAsync(
        Guid schoolId, string requestKey, string fingerprint, SlotResourceKind? resourceKind,
        Guid? resourceId,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> occurrences,
        Func<CancellationToken, Task<InterviewAssessmentSlotGenerationBatch>> mutation,
        CancellationToken ct = default)
    {
        var outcome = new AtomicSlotGenerationOutcome(AtomicSlotGenerationResult.ConcurrencyConflict);
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
            try
            {
                await AcquireApplicationLockAsync($"slot-generation:{schoolId:N}:{requestKey}", ct);
                var existing = await db.InterviewAssessmentSlotGenerationBatches.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.SchoolId == schoolId && x.RequestKey == requestKey, ct);
                if (existing is not null)
                {
                    outcome = existing.RequestFingerprint == fingerprint
                        ? new(AtomicSlotGenerationResult.Existing, existing.BatchReference)
                        : new(AtomicSlotGenerationResult.IdempotencyConflict);
                    await transaction.CommitAsync(ct);
                    return;
                }

                if (resourceKind.HasValue && resourceId.HasValue)
                {
                    await AcquireApplicationLockAsync(
                        $"slot-open:{schoolId:N}:{(int)resourceKind}:{resourceId:N}", ct);
                    foreach (var occurrence in occurrences)
                    {
                        if (await db.InterviewAssessmentSlots.AsNoTracking().AnyAsync(x =>
                            x.SchoolId == schoolId && x.Status == SlotStatus.Open &&
                            x.ResourceKind == resourceKind && x.ResourceReferenceId == resourceId &&
                            x.StartAtUtc < occurrence.End && x.EndAtUtc > occurrence.Start, ct))
                        {
                            outcome = new(AtomicSlotGenerationResult.ResourceConflict);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                    }
                }

                var batch = await mutation(ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                outcome = new(AtomicSlotGenerationResult.Created, batch.BatchReference);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                outcome = new(AtomicSlotGenerationResult.ConcurrencyConflict);
            }
        });
        return outcome;
    }

    private Task AcquireApplicationLockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            IF @lockResult < 0 THROW 51000, 'Unable to acquire slot lock.', 1;
            """, ct);

    public async Task<int> CountActiveAppointmentsAsync(Guid id, CancellationToken ct = default) =>
        await db.AdmissionInterviewAppointments.CountAsync(x => x.InterviewAssessmentSlotId == id &&
            (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.InProgress), ct) +
        await db.AdmissionAssessmentAppointments.CountAsync(x => x.InterviewAssessmentSlotId == id &&
            (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.InProgress), ct);

    public async Task<int> CountAffectedApplicationsAsync(Guid id, CancellationToken ct = default)
    {
        var i = db.AdmissionInterviewAppointments.Where(x => x.InterviewAssessmentSlotId == id &&
            (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.InProgress)).Select(x => x.AdmissionApplicationId);
        var a = db.AdmissionAssessmentAppointments.Where(x => x.InterviewAssessmentSlotId == id &&
            (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.InProgress)).Select(x => x.AdmissionApplicationId);
        return await i.Concat(a).Distinct().CountAsync(ct);
    }

    public async Task<bool> BranchScopeIsValidAsync(Guid schoolId, Guid branchId, Guid stageId,
        Guid? gradeId, Guid yearId, CancellationToken ct = default)
    {
        var branch = await db.SchoolBranches.AsNoTracking().AnyAsync(x =>
            x.Id == branchId && x.SchoolId == schoolId && x.IsActive, ct);
        var stage = await db.EducationalStages.AsNoTracking().AnyAsync(x => x.Id == stageId, ct);
        var grade = !gradeId.HasValue || await db.Grades.AsNoTracking().AnyAsync(x =>
            x.Id == gradeId && x.EducationalStageId == stageId, ct);
        var year = await db.AcademicYears.AsNoTracking().AnyAsync(x => x.Id == yearId && x.IsActive, ct);
        return branch && stage && grade && year;
    }

    public Task<bool> ResourceIsValidAsync(Guid schoolId, Guid branchId, Guid resourceId,
        CancellationToken ct = default) =>
        db.SchoolTeamMembers.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.SchoolId == schoolId &&
            x.IsActive && (x.BranchScopeMode == SchoolBranchScopeMode.AllBranches ||
                x.BranchAssignments.Any(b => b.SchoolBranchId == branchId)), ct);

    public async Task<bool> MeetingProviderIsActiveAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpper();
        var integration = await db.PlatformIntegrationConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IntegrationType == IntegrationType.Meeting && x.IsActive &&
                x.ProviderCode.ToUpper() == normalized, ct);
        return integration is not null && integrationSettingsValidator.Validate(
            integration.IntegrationType, integration.ProviderCode, integration.SettingsJson,
            integration.SettingsSchemaVersion).IsValid;
    }
    public Task<AcademicYear?> GetAcademicYearAsync(Guid id, CancellationToken ct = default) =>
        db.AcademicYears.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<SchoolBranch?> GetBranchAsync(Guid schoolId, Guid id, CancellationToken ct = default) =>
        db.SchoolBranches.AsNoTracking().FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, ct);
    public Task<InterviewAssessmentSlotGenerationBatch?> GetBatchAsync(Guid schoolId, string key,
        CancellationToken ct = default) => db.InterviewAssessmentSlotGenerationBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.RequestKey == key, ct);
    public Task AddBatchAsync(InterviewAssessmentSlotGenerationBatch batch, CancellationToken ct = default) =>
        db.InterviewAssessmentSlotGenerationBatches.AddAsync(batch, ct).AsTask();
    public async Task<IReadOnlyList<InterviewAssessmentSlot>> ListBatchAsync(Guid schoolId, string reference,
        CancellationToken ct = default) => await db.InterviewAssessmentSlots.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.GenerationBatchReference == reference)
            .OrderBy(x => x.StartAtUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<AdmissionApplication>> GetApplicationsForCancellationAsync(
        Guid slotId, SlotKind kind, CancellationToken ct = default)
    {
        var q = db.AdmissionApplications.Include(x => x.InterviewAppointments)
            .Include(x => x.AssessmentAppointments).Include(x => x.History);
        return kind == SlotKind.Interview
            ? await q.Where(x => x.InterviewAppointments.Any(a => a.InterviewAssessmentSlotId == slotId &&
                (a.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
                 a.Lifecycle == AdmissionAppointmentLifecycle.Confirmed))).ToListAsync(ct)
            : await q.Where(x => x.AssessmentAppointments.Any(a => a.InterviewAssessmentSlotId == slotId &&
                (a.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
                 a.Lifecycle == AdmissionAppointmentLifecycle.Confirmed))).ToListAsync(ct);
    }

    public Task<AdmissionApplication?> GetOwnedApplicationAsync(Guid parentUserId, Guid applicationId,
        CancellationToken ct = default) => db.AdmissionApplications.AsNoTracking()
            .Include(x => x.PolicySnapshot).FirstOrDefaultAsync(x =>
                x.Id == applicationId && x.ParentUserId == parentUserId, ct);

    public Task AddAppointmentActionAsync(
        AdmissionAppointmentActionHistory action, CancellationToken ct = default) =>
        db.AdmissionAppointmentActionHistory.AddAsync(action, ct).AsTask();

    public async Task<ParentAppointmentActionOutcome> ExecuteParentAppointmentActionAsync(
        ParentAppointmentActionRequest request, CancellationToken ct = default)
    {
        var outcome = new ParentAppointmentActionOutcome(
            ParentAppointmentActionResult.ConcurrencyConflict);
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
            try
            {
                await AcquireApplicationLockAsync($"parent-appointment:{request.ApplicationId:N}", ct);
                var application = await db.AdmissionApplications
                    .FromSqlInterpolated($"""
                        SELECT * FROM [AdmissionApplications] WITH (UPDLOCK,HOLDLOCK)
                        WHERE [Id] = {request.ApplicationId} AND [ParentUserId] = {request.ParentUserId}
                        """)
                    .Include(x => x.PolicySnapshot)
                    .Include(x => x.InterviewAppointments)
                    .Include(x => x.AssessmentAppointments)
                    .SingleOrDefaultAsync(ct);
                var expectedStatus = request.Kind == SlotKind.Interview
                    ? AdmissionApplicationStatus.InterviewRequired
                    : AdmissionApplicationStatus.AssessmentRequired;
                if (application is null || application.Status != expectedStatus ||
                    application.PolicySnapshot is null || !Enum.IsDefined(request.Kind))
                {
                    outcome = new(ParentAppointmentActionResult.NotFound);
                    await transaction.RollbackAsync(ct);
                    return;
                }

                var appointment = AppointmentState.For(application, request.Kind);
                if (appointment is null)
                {
                    outcome = new(ParentAppointmentActionResult.NotFound);
                    await transaction.RollbackAsync(ct);
                    return;
                }
                var idempotencyKey = Normalize(request.IdempotencyKey, 128);
                var safeReason = Normalize(request.Reason, 500);
                if (request.Action != ParentAppointmentActionKind.Join)
                {
                    if (idempotencyKey is null)
                    {
                        outcome = new(ParentAppointmentActionResult.IdempotencyConflict);
                        await transaction.RollbackAsync(ct);
                        return;
                    }
                    var existing = await db.AdmissionAppointmentActionHistory.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.AppointmentId == appointment.Id &&
                            x.IdempotencyKey == idempotencyKey, ct);
                    if (existing is not null)
                    {
                        var expectedAction = ToTimelineAction(request.Action);
                        outcome = existing.Action == expectedAction &&
                                  existing.NewSlotId == (request.SlotId ?? appointment.SlotId) &&
                                  existing.SafeReason == safeReason
                            ? new(ParentAppointmentActionResult.Existing, application)
                            : new(ParentAppointmentActionResult.IdempotencyConflict);
                        await transaction.CommitAsync(ct);
                        return;
                    }
                }
                if (request.RowVersion is null ||
                    !request.RowVersion.AsSpan().SequenceEqual(appointment.RowVersion))
                {
                    outcome = new(ParentAppointmentActionResult.ConcurrencyConflict);
                    await transaction.RollbackAsync(ct);
                    return;
                }

                var policy = application.PolicySnapshot;
                var oldLifecycle = appointment.Lifecycle;
                var oldSlotId = appointment.SlotId;
                var lockSlotIds = new[] { oldSlotId, request.SlotId }
                    .Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x);
                foreach (var lockSlotId in lockSlotIds)
                    await AcquireApplicationLockAsync($"appointment-slot:{lockSlotId:N}", ct);
                InterviewAssessmentSlot? currentSlot = null;
                if (oldSlotId.HasValue)
                    currentSlot = await LockSlotAsync(oldSlotId.Value, ct);
                var deadline = currentSlot?.StartAtUtc.AddHours(
                    -(policy.MinimumSchedulingLeadTimeHours ?? 0));

                if (request.Action == ParentAppointmentActionKind.Join)
                {
                    var joinResult = await ValidateJoinAsync(
                        appointment, currentSlot, policy, ct);
                    outcome = new(joinResult, application);
                    await transaction.RollbackAsync(ct);
                    return;
                }

                AdmissionAppointmentAction timelineAction;
                switch (request.Action)
                {
                    case ParentAppointmentActionKind.Confirm:
                        if (appointment.Lifecycle != AdmissionAppointmentLifecycle.Proposed)
                        {
                            outcome = new(ParentAppointmentActionResult.InvalidTransition);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (currentSlot is null ||
                            currentSlot.Id != appointment.SlotId ||
                            currentSlot.Status is SlotStatus.Draft or SlotStatus.Cancelled ||
                            currentSlot.SchoolId != application.SchoolId ||
                            currentSlot.SchoolBranchId != application.SchoolBranchId ||
                            currentSlot.EducationalStageId != application.EducationalStageId ||
                            currentSlot.GradeId != application.GradeId ||
                            currentSlot.AcademicYearId != application.AcademicYearId ||
                            currentSlot.Kind != request.Kind ||
                            currentSlot.StartAtUtc != appointment.ScheduledAtUtc ||
                            !string.Equals(currentSlot.TimeZoneId, appointment.TimeZoneId,
                                StringComparison.Ordinal) ||
                            (currentSlot.DeliveryMode == SlotDeliveryMode.Online) !=
                                (appointment.Mode == AdmissionAppointmentMode.Online))
                        {
                            outcome = new(ParentAppointmentActionResult.SlotUnavailable);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (DateTimeOffset.UtcNow >= currentSlot.StartAtUtc)
                        {
                            outcome = new(ParentAppointmentActionResult.DeadlinePassed);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        appointment.Confirm();
                        timelineAction = AdmissionAppointmentAction.Confirmed;
                        break;

                    case ParentAppointmentActionKind.SelectSlot:
                        if (!request.SlotId.HasValue ||
                            appointment.Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
                                AdmissionAppointmentLifecycle.Confirmed or
                                AdmissionAppointmentLifecycle.RescheduleRequested))
                        {
                            outcome = new(ParentAppointmentActionResult.InvalidTransition);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        var schoolRecovery =
                            appointment.Lifecycle == AdmissionAppointmentLifecycle.RescheduleRequested &&
                            appointment.RescheduleInitiator == AppointmentRescheduleInitiator.School;
                        if (!schoolRecovery && !policy.ParentReschedulingAllowed)
                        {
                            outcome = new(ParentAppointmentActionResult.RescheduleNotAllowed);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        var changesActiveSlot = appointment.IsActiveReservation &&
                            appointment.SlotId != request.SlotId;
                        if (changesActiveSlot &&
                            appointment.AttemptCount >= policy.MaxParentRescheduleAttempts)
                        {
                            outcome = new(ParentAppointmentActionResult.RescheduleLimitReached);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (changesActiveSlot && deadline.HasValue && DateTimeOffset.UtcNow > deadline)
                        {
                            outcome = new(ParentAppointmentActionResult.DeadlinePassed);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        var destination = request.SlotId == oldSlotId
                            ? currentSlot
                            : await LockSlotAsync(request.SlotId.Value, ct);
                        if (!await IsEligibleDestinationAsync(
                                destination, application, policy, request.Kind, ct))
                        {
                            outcome = new(ParentAppointmentActionResult.SlotUnavailable);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        var occupied = await CountReservationsAsync(
                            destination!.Id, appointment.Id, ct);
                        if (occupied >= destination.Capacity)
                        {
                            outcome = new(ParentAppointmentActionResult.SlotFull);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        appointment.Select(destination);
                        timelineAction = AdmissionAppointmentAction.AlternateSlotSelected;
                        break;

                    case ParentAppointmentActionKind.RequestReschedule:
                        if (!policy.ParentReschedulingAllowed)
                        {
                            outcome = new(ParentAppointmentActionResult.RescheduleNotAllowed);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (appointment.Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
                            AdmissionAppointmentLifecycle.Confirmed))
                        {
                            outcome = new(ParentAppointmentActionResult.InvalidTransition);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (appointment.AttemptCount >= policy.MaxParentRescheduleAttempts)
                        {
                            outcome = new(ParentAppointmentActionResult.RescheduleLimitReached);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (deadline.HasValue && DateTimeOffset.UtcNow > deadline)
                        {
                            outcome = new(ParentAppointmentActionResult.DeadlinePassed);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        appointment.RequestReschedule(safeReason);
                        timelineAction = AdmissionAppointmentAction.RescheduleRequested;
                        break;

                    case ParentAppointmentActionKind.Cancel:
                        if (!policy.ParentCancellationAllowed)
                        {
                            outcome = new(ParentAppointmentActionResult.CancellationNotAllowed);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (appointment.Lifecycle is not (AdmissionAppointmentLifecycle.Proposed or
                            AdmissionAppointmentLifecycle.Confirmed or
                            AdmissionAppointmentLifecycle.RescheduleRequested))
                        {
                            outcome = new(ParentAppointmentActionResult.InvalidTransition);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        if (appointment.IsActiveReservation && deadline.HasValue &&
                            DateTimeOffset.UtcNow > deadline)
                        {
                            outcome = new(ParentAppointmentActionResult.DeadlinePassed);
                            await transaction.RollbackAsync(ct);
                            return;
                        }
                        appointment.Cancel(safeReason);
                        timelineAction = AdmissionAppointmentAction.Cancelled;
                        break;

                    default:
                        outcome = new(ParentAppointmentActionResult.InvalidTransition);
                        await transaction.RollbackAsync(ct);
                        return;
                }

                if (appointment.Lifecycle == AdmissionAppointmentLifecycle.Confirmed)
                {
                    await meetingSessions.EnsureForConfirmedAppointmentAsync(
                        application.Id, appointment.Id, request.Kind, ct);
                }
                else if (oldLifecycle == AdmissionAppointmentLifecycle.Confirmed)
                {
                    await meetingSessions.HandleAppointmentUnavailableAsync(
                        appointment.Id, request.Kind, request.ParentUserId,
                        $"appointment:{appointment.Id:N}:{idempotencyKey}", ct);
                }

                var actionHistory = new AdmissionAppointmentActionHistory(
                    application.Id, appointment.Id, request.Kind, timelineAction,
                    oldLifecycle, appointment.Lifecycle, oldSlotId, appointment.SlotId,
                    AdmissionAppointmentActorType.Parent, request.ParentUserId,
                    safeReason, idempotencyKey);
                db.AdmissionAppointmentActionHistory.Add(actionHistory);
                db.AdmissionApplicationHistory.Add(new AdmissionApplicationHistory(
                    application.Id, application.Status, application.Status,
                    timelineAction.ToString(), request.ParentUserId, "Parent",
                    true, safeReason, null));
                await EnqueueAppointmentNotificationsAsync(
                    application, timelineAction, actionHistory.Id, ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                outcome = new(ParentAppointmentActionResult.Succeeded, application);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(ct);
                outcome = new(ParentAppointmentActionResult.ConcurrencyConflict);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                outcome = new(ParentAppointmentActionResult.ConcurrencyConflict);
            }
        });
        return outcome;
    }

    private async Task EnqueueAppointmentNotificationsAsync(
        AdmissionApplication application,
        AdmissionAppointmentAction action,
        Guid actionHistoryId,
        CancellationToken ct)
    {
        var parentEvent = action switch
        {
            AdmissionAppointmentAction.Confirmed => NotificationEventType.AppointmentConfirmed,
            AdmissionAppointmentAction.AlternateSlotSelected => NotificationEventType.AppointmentChanged,
            AdmissionAppointmentAction.RescheduleRequested => NotificationEventType.AppointmentChanged,
            AdmissionAppointmentAction.Cancelled => NotificationEventType.AppointmentCancelled,
            _ => NotificationEventType.AppointmentChanged,
        };
        var variables = new Dictionary<string, string>
        {
            ["applicationNumber"] = application.ApplicationNumber,
            ["action"] = action.ToString(),
        };
        var parentCulture = await db.Users.AsNoTracking()
            .Where(x => x.Id == application.ParentUserId)
            .Select(x => x.PreferredLanguage).SingleOrDefaultAsync(ct);
        await notificationOutboxPublisher.EnqueueAsync(new(
            application.ParentUserId, parentEvent,
            parentCulture?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "ar",
            $"appointment:{actionHistoryId:N}:recipient:{application.ParentUserId:N}",
            variables, $"/parent/applications/{application.Id}", application.SchoolId,
            application.Id, [NotificationChannel.InApp], SkipPreferenceCheck: true), ct);

        var schoolEvent = action switch
        {
            AdmissionAppointmentAction.Confirmed => NotificationEventType.ParentAppointmentConfirmed,
            AdmissionAppointmentAction.AlternateSlotSelected => NotificationEventType.ParentAlternateSlotSelected,
            AdmissionAppointmentAction.RescheduleRequested => NotificationEventType.ParentAppointmentRescheduleRequested,
            AdmissionAppointmentAction.Cancelled => NotificationEventType.ParentAppointmentCancelled,
            _ => NotificationEventType.ParentAppointmentConfirmed,
        };
        var ownerId = await db.Schools.AsNoTracking().Where(x => x.Id == application.SchoolId)
            .Select(x => x.OwnerUserId).SingleAsync(ct);
        var memberIds = await db.SchoolTeamMembers.AsNoTracking()
            .Where(x => x.SchoolId == application.SchoolId && x.IsActive &&
                (x.Role == SchoolTeamRole.SchoolAdmin ||
                 x.Role == SchoolTeamRole.AdmissionOfficer) &&
                (x.BranchScopeMode == SchoolBranchScopeMode.AllBranches ||
                 x.BranchAssignments.Any(b =>
                     b.SchoolBranchId == application.SchoolBranchId)))
            .Select(x => x.UserId).ToListAsync(ct);
        if (ownerId.HasValue) memberIds.Add(ownerId.Value);
        var recipients = memberIds.Distinct().ToArray();
        var recipientCultures = await db.Users.AsNoTracking()
            .Where(x => recipients.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PreferredLanguage, ct);
        foreach (var recipient in recipients)
            await notificationOutboxPublisher.EnqueueAsync(new(
                recipient, schoolEvent,
                recipientCultures.GetValueOrDefault(recipient)?.StartsWith(
                    "en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "ar",
                $"appointment:{actionHistoryId:N}:recipient:{recipient:N}",
                variables,
                $"/school/{application.SchoolId}/applications/{application.Id}",
                application.SchoolId, application.Id,
                [NotificationChannel.InApp], SkipPreferenceCheck: true), ct);
    }

    private async Task<InterviewAssessmentSlot?> LockSlotAsync(Guid slotId, CancellationToken ct) =>
        await db.InterviewAssessmentSlots.FromSqlInterpolated($"""
            SELECT * FROM [InterviewAssessmentSlots] WITH (UPDLOCK,HOLDLOCK)
            WHERE [Id] = {slotId}
            """).SingleOrDefaultAsync(ct);

    private async Task<int> CountReservationsAsync(
        Guid slotId, Guid excludeAppointmentId, CancellationToken ct) =>
        await db.AdmissionInterviewAppointments.CountAsync(x =>
            x.InterviewAssessmentSlotId == slotId && x.Id != excludeAppointmentId &&
            (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.InProgress), ct) +
        await db.AdmissionAssessmentAppointments.CountAsync(x =>
            x.InterviewAssessmentSlotId == slotId && x.Id != excludeAppointmentId &&
            (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.InProgress), ct);

    private async Task<bool> IsEligibleDestinationAsync(
        InterviewAssessmentSlot? slot,
        AdmissionApplication application,
        AdmissionApplicationInterviewAssessmentPolicySnapshot policy,
        SlotKind kind,
        CancellationToken ct)
    {
        if (slot is null || slot.Status != SlotStatus.Open || slot.Kind != kind ||
            slot.SchoolId != application.SchoolId ||
            slot.SchoolBranchId != application.SchoolBranchId ||
            slot.EducationalStageId != application.EducationalStageId ||
            slot.GradeId != application.GradeId ||
            slot.AcademicYearId != application.AcademicYearId ||
            slot.StartAtUtc <= DateTimeOffset.UtcNow.AddHours(
                policy.MinimumSchedulingLeadTimeHours ?? 0) ||
            (int)(slot.EndAtUtc - slot.StartAtUtc).TotalMinutes != policy.ExpectedDurationMinutes)
            return false;

        var academicYear = await GetAcademicYearAsync(application.AcademicYearId, ct);
        if (academicYear is null) return false;
        var yearStart = new DateTimeOffset(
            academicYear.StartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        if ((policy.BookingWindowOpensDaysBefore is { } opens &&
             slot.StartAtUtc < yearStart.AddDays(-opens)) ||
            (policy.BookingWindowClosesDaysBefore is { } closes &&
             slot.StartAtUtc > yearStart.AddDays(-closes)))
            return false;

        var modeAllowed = slot.DeliveryMode == SlotDeliveryMode.Online
            ? policy.DeliveryMode is InterviewAssessmentDeliveryMode.Online or
                InterviewAssessmentDeliveryMode.Hybrid
            : policy.DeliveryMode is InterviewAssessmentDeliveryMode.OnSite or
                InterviewAssessmentDeliveryMode.Hybrid;
        if (!modeAllowed) return false;
        if (slot.DeliveryMode == SlotDeliveryMode.Online)
            return !string.IsNullOrWhiteSpace(slot.MeetingProviderCode) &&
                   string.Equals(slot.MeetingProviderCode, policy.MeetingProviderCode,
                       StringComparison.OrdinalIgnoreCase) &&
                   await MeetingProviderIsActiveAsync(slot.MeetingProviderCode, ct);
        var branch = await GetBranchAsync(application.SchoolId, slot.SchoolBranchId, ct);
        return branch is not null && branch.IsActive &&
               (!string.IsNullOrWhiteSpace(branch.AddressLineAr) ||
                !string.IsNullOrWhiteSpace(branch.AddressReference));
    }

    private async Task<ParentAppointmentActionResult> ValidateJoinAsync(
        AppointmentState appointment,
        InterviewAssessmentSlot? slot,
        AdmissionApplicationInterviewAssessmentPolicySnapshot policy,
        CancellationToken ct)
    {
        if (appointment.Lifecycle != AdmissionAppointmentLifecycle.Confirmed ||
            appointment.Mode != AdmissionAppointmentMode.Online || slot is null ||
            slot.Status == SlotStatus.Cancelled || slot.Id != appointment.SlotId)
            return ParentAppointmentActionResult.InvalidTransition;
        if (
            string.IsNullOrWhiteSpace(slot.MeetingProviderCode) ||
            !string.Equals(slot.MeetingProviderCode, policy.MeetingProviderCode,
                StringComparison.OrdinalIgnoreCase) ||
            !await MeetingProviderIsActiveAsync(slot.MeetingProviderCode, ct))
            return ParentAppointmentActionResult.JoinProviderUnavailable;
        var now = DateTimeOffset.UtcNow;
        if (now < slot.StartAtUtc.AddMinutes(-15))
            return ParentAppointmentActionResult.JoinTooEarly;
        if (now > slot.EndAtUtc)
            return ParentAppointmentActionResult.JoinExpired;
        return ParentAppointmentActionResult.JoinProviderUnavailable;
    }

    private static AdmissionAppointmentAction ToTimelineAction(ParentAppointmentActionKind action) =>
        action switch
        {
            ParentAppointmentActionKind.Confirm => AdmissionAppointmentAction.Confirmed,
            ParentAppointmentActionKind.SelectSlot => AdmissionAppointmentAction.AlternateSlotSelected,
            ParentAppointmentActionKind.RequestReschedule => AdmissionAppointmentAction.RescheduleRequested,
            ParentAppointmentActionKind.Cancel => AdmissionAppointmentAction.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var plain = value.Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal).Trim();
        return plain[..Math.Min(max, plain.Length)];
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
                    .FirstOrDefault(x => x.Lifecycle is AdmissionAppointmentLifecycle.Proposed or
                        AdmissionAppointmentLifecycle.Confirmed or
                        AdmissionAppointmentLifecycle.RescheduleRequested);
                return value is null ? null : new(value);
            }
            var assessment = application.AssessmentAppointments.OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault(x => x.Lifecycle is AdmissionAppointmentLifecycle.Proposed or
                    AdmissionAppointmentLifecycle.Confirmed or
                    AdmissionAppointmentLifecycle.RescheduleRequested);
            return assessment is null ? null : new(assessment);
        }

        public Guid Id => interview?.Id ?? assessment!.Id;
        public Guid? SlotId => interview?.InterviewAssessmentSlotId ??
            assessment?.InterviewAssessmentSlotId;
        public AdmissionAppointmentLifecycle Lifecycle =>
            interview?.Lifecycle ?? assessment!.Lifecycle;
        public AdmissionAppointmentMode Mode => interview?.Mode ?? assessment!.Mode;
        public DateTimeOffset ScheduledAtUtc =>
            interview?.ScheduledAtUtc ?? assessment!.ScheduledAtUtc;
        public string TimeZoneId => interview?.TimeZoneId ?? assessment!.TimeZoneId;
        public byte[] RowVersion => interview?.RowVersion ?? assessment!.RowVersion;
        public int AttemptCount => interview?.ParentRescheduleAttemptCount ??
            assessment!.ParentRescheduleAttemptCount;
        public AppointmentRescheduleInitiator? RescheduleInitiator =>
            interview?.RescheduleInitiator ?? assessment?.RescheduleInitiator;
        public bool IsActiveReservation => interview?.IsActiveReservation ??
            assessment!.IsActiveReservation;

        public void Confirm()
        {
            if (interview is not null) interview.ConfirmCurrent();
            else assessment!.ConfirmCurrent();
        }

        public void Select(InterviewAssessmentSlot slot)
        {
            var mode = slot.DeliveryMode == SlotDeliveryMode.Online
                ? AdmissionAppointmentMode.Online : AdmissionAppointmentMode.InPerson;
            if (interview is not null)
                interview.SelectAlternate(slot.StartAtUtc, slot.TimeZoneId, mode,
                    mode == AdmissionAppointmentMode.InPerson ? slot.InstructionsAr : null,
                    mode == AdmissionAppointmentMode.Online ? slot.InstructionsAr : null,
                    null, slot.InstructionsAr, slot.Id);
            else
                assessment!.SelectAlternate(slot.StartAtUtc, slot.TimeZoneId, mode,
                    mode == AdmissionAppointmentMode.InPerson ? slot.InstructionsAr : null,
                    mode == AdmissionAppointmentMode.Online ? slot.InstructionsAr : null,
                    null, slot.InstructionsAr, slot.Id);
        }

        public void RequestReschedule(string? reason)
        {
            if (interview is not null)
                interview.RequestReschedule(reason, AppointmentRescheduleInitiator.Parent);
            else assessment!.RequestReschedule(reason, AppointmentRescheduleInitiator.Parent);
        }

        public void Cancel(string? reason)
        {
            if (interview is not null) interview.CancelByParent(reason);
            else assessment!.CancelByParent(reason);
        }
    }
}
