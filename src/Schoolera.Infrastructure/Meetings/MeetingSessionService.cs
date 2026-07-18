using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Application.Meetings;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Meetings;

public sealed class MeetingSessionService(
    SchooleraDbContext db,
    IIntegrationSettingsValidator settingsValidator,
    IRuntimeEnvironment runtimeEnvironment,
    IEnumerable<IMeetingProvider> providers,
    INotificationOutboxPublisher notificationOutbox,
    ILogger<MeetingSessionService> logger) : IMeetingSessionService
{
    private readonly IReadOnlyList<IMeetingProvider> meetingProviders = providers.ToArray();

    public async Task EnsureForConfirmedAppointmentAsync(
        Guid applicationId, Guid appointmentId, SlotKind kind, CancellationToken ct = default)
    {
        var state = await LoadAppointmentAsync(applicationId, appointmentId, kind, true, ct);
        if (state is null || state.Lifecycle != AdmissionAppointmentLifecycle.Confirmed ||
            !state.MatchesWorkflow)
            return;
        var latest = await db.AdmissionMeetingSessions
            .Where(x => x.AppointmentId == appointmentId && x.Kind == kind)
            .OrderByDescending(x => x.Generation).FirstOrDefaultAsync(ct);
        if (latest is not null &&
            latest.ScheduledStartAtUtc == state.StartsAtUtc &&
            latest.ScheduledEndAtUtc == state.EndsAtUtc &&
            state.Mode == AdmissionAppointmentMode.Online &&
            latest.Status is not (MeetingSessionStatus.Cancelled or MeetingSessionStatus.Expired))
            return;
        if (latest is not null && !latest.IsTerminal)
        {
            await HandleAppointmentUnavailableAsync(
                appointmentId, kind, null,
                $"meeting:{latest.Id:N}:schedule-replaced", ct);
        }
        if (state.Mode != AdmissionAppointmentMode.Online)
            return;

        var policyProviderCode = state.Application.PolicySnapshot?.MeetingProviderCode;
        if (string.IsNullOrWhiteSpace(policyProviderCode))
            return;
        var simulatedPolicyProvider = IntegrationProviderCodes.IsSimulated(policyProviderCode);
        var integration = await db.PlatformIntegrationConfigurations
            .Where(x => x.IntegrationType == IntegrationType.Meeting && x.IsActive && x.IsDefault &&
                x.HealthStatus != IntegrationHealthStatus.Unhealthy &&
                (x.ProviderCode == policyProviderCode ||
                 simulatedPolicyProvider &&
                 (x.ProviderCode == IntegrationProviderCodes.Simulated ||
                  x.ProviderCode == IntegrationProviderCodes.Development)))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (integration is null || !TrySettings(integration, out var settings, out var environment) ||
            !RuntimeAllows(environment) || !settings.SupportsMeetingCreation)
            return;

        var session = new AdmissionMeetingSession(
            applicationId, appointmentId, kind, state.Application.SchoolId,
            state.Application.SchoolBranchId, integration.Id, integration.ProviderCode,
            environment, integration.SettingsSchemaVersion, integration.RowVersion,
            (latest?.Generation ?? 0) + 1,
            $"meeting:{appointmentId:N}:{(int)kind}:{(latest?.Generation ?? 0) + 1}",
            state.StartsAtUtc, state.EndsAtUtc,
            state.EndsAtUtc.AddMinutes(settings.JoinAfterMinutes));
        db.AdmissionMeetingSessions.Add(session);
        db.AdmissionMeetingSessionHistory.Add(new(
            session.Id, MeetingSessionHistoryAction.ProvisioningRequested,
            MeetingSessionStatus.PendingProvisioning, MeetingSessionStatus.PendingProvisioning,
            "meeting.provisioning.requested", null, $"meeting:{session.Id:N}:requested"));
    }

    public async Task HandleAppointmentUnavailableAsync(
        Guid appointmentId, SlotKind kind, Guid? actorUserId, string idempotencyKey,
        CancellationToken ct = default)
    {
        var sessions = await db.AdmissionMeetingSessions.Where(
            x => x.AppointmentId == appointmentId && x.Kind == kind &&
                x.Status != MeetingSessionStatus.Cancelled &&
                x.Status != MeetingSessionStatus.Expired).ToListAsync(ct);
        foreach (var session in sessions)
        {
            var old = session.Status;
            var settings = await GetHistoricalSettingsAsync(session, requireActive: false, ct);
            session.RequestCancellation(
                old == MeetingSessionStatus.Ready && settings?.SupportsCancellation == true);
            db.AdmissionMeetingSessionHistory.Add(new(
                session.Id, MeetingSessionHistoryAction.CancellationRequested, old, session.Status,
                "meeting.cancellation.requested", actorUserId,
                NormalizeKey($"{idempotencyKey}:{session.Generation}",
                    $"meeting:{session.Id:N}:cancel")));
        }
    }

    public async Task HandleAppointmentEndedAsync(
        Guid appointmentId, SlotKind kind, Guid? actorUserId, string idempotencyKey,
        CancellationToken ct = default)
    {
        var sessions = await db.AdmissionMeetingSessions.Where(
            x => x.AppointmentId == appointmentId && x.Kind == kind &&
                x.Status != MeetingSessionStatus.Cancelled &&
                x.Status != MeetingSessionStatus.Expired).ToListAsync(ct);
        foreach (var session in sessions)
        {
            if (session.Status == MeetingSessionStatus.Ready)
            {
                var old = session.Status;
                session.MarkExpired();
                db.AdmissionMeetingSessionHistory.Add(new(
                    session.Id, MeetingSessionHistoryAction.Expired, old, session.Status,
                    "meeting.appointment.ended", actorUserId,
                    NormalizeKey($"{idempotencyKey}:{session.Generation}",
                        $"meeting:{session.Id:N}:ended")));
            }
            else
            {
                var old = session.Status;
                session.RequestCancellation(false);
                db.AdmissionMeetingSessionHistory.Add(new(
                    session.Id, MeetingSessionHistoryAction.CancellationRequested,
                    old, session.Status, "meeting.appointment.ended", actorUserId,
                    NormalizeKey($"{idempotencyKey}:{session.Generation}",
                        $"meeting:{session.Id:N}:ended")));
            }
        }
    }

    public async Task ProcessDueSessionsAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var ids = await db.AdmissionMeetingSessions.AsNoTracking()
            .Where(x =>
                (x.Status == MeetingSessionStatus.PendingProvisioning &&
                    (!x.NextRetryAtUtc.HasValue || x.NextRetryAtUtc <= now)) ||
                (x.Status == MeetingSessionStatus.ProvisioningFailed &&
                    x.NextRetryAtUtc.HasValue && x.NextRetryAtUtc <= now) ||
                x.Status == MeetingSessionStatus.CancellationPending ||
                (x.Status == MeetingSessionStatus.Ready && x.ExpirationAtUtc <= now) ||
                (x.Status == MeetingSessionStatus.Ready && x.CancellationRequestedAtUtc.HasValue &&
                    x.NextRetryAtUtc.HasValue && x.NextRetryAtUtc <= now))
            .OrderBy(x => x.UpdatedAtUtc).Select(x => x.Id).Take(20).ToListAsync(ct);
        foreach (var id in ids)
        {
            try { await ProcessOneAsync(id, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Meeting session operation failed for {MeetingSessionId}.", id);
            }
        }
    }

    public async Task<MeetingSessionSummaryDto?> GetSummaryAsync(
        Guid appointmentId, SlotKind kind, bool forSchool, CancellationToken ct = default)
    {
        var state = await LoadAppointmentByIdAsync(appointmentId, kind, false, ct);
        if (state is null) return null;
        var session = await db.AdmissionMeetingSessions.AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId && x.Kind == kind)
            .OrderByDescending(x => x.Generation).FirstOrDefaultAsync(ct);
        if (session is null)
        {
            if (state.Mode != AdmissionAppointmentMode.Online) return null;
            return new(
                MeetingSessionStatus.ProvisioningFailed,
                "meeting.provider.notConfigured",
                null,
                state.EndsAtUtc,
                null,
                false,
                false,
                false,
                false,
                forSchool,
                "meeting.provider.notConfigured",
                null,
                null,
                null);
        }
        var integration = await db.PlatformIntegrationConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == session.IntegrationConfigurationId, ct);
        MeetingIntegrationSettings? settings = null;
        MeetingProviderEnvironment environment = session.ProviderEnvironment;
        if (integration is not null) TrySettings(integration, out settings, out environment);
        return BuildSummary(session, state, integration, settings, forSchool);
    }

    public async Task<MeetingAccessActionDto?> ResolveParentAccessAsync(
        Guid parentUserId, Guid applicationId, Guid appointmentId, SlotKind kind,
        CancellationToken ct = default)
    {
        var state = await LoadAppointmentAsync(applicationId, appointmentId, kind, true, ct);
        if (state is null || state.Application.ParentUserId != parentUserId ||
            state.Lifecycle != AdmissionAppointmentLifecycle.Confirmed ||
            state.Mode != AdmissionAppointmentMode.Online || !state.MatchesWorkflow)
            return null;
        var resolved = await ResolveReadyAsync(appointmentId, kind, state, false, ct);
        if (resolved is null || !resolved.Settings.SupportsParentAccess) return null;
        var now = DateTimeOffset.UtcNow;
        var starts = state.StartsAtUtc.AddMinutes(-resolved.Settings.JoinBeforeMinutes);
        var ends = state.EndsAtUtc.AddMinutes(resolved.Settings.JoinAfterMinutes);
        if (now < starts || now > ends) return null;
        var action = await resolved.Provider.ResolveParentAccessAsync(resolved.Context, ct);
        if (action is null) return null;
        db.AdmissionMeetingSessionHistory.Add(new(
            resolved.Session.Id, MeetingSessionHistoryAction.ParentAccessIssued,
            resolved.Session.Status, resolved.Session.Status, "meeting.parentAccess.issued",
            parentUserId, $"meeting:{resolved.Session.Id:N}:parent:{Guid.NewGuid():N}"));
        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Issued safe Parent meeting access for session {MeetingSessionId}.",
            resolved.Session.Id);
        return action;
    }

    public async Task<MeetingAccessActionDto?> ResolveSchoolHostAccessAsync(
        Guid schoolId, Guid actorUserId, Guid appointmentId, SlotKind kind,
        CancellationToken ct = default)
    {
        var state = await LoadAppointmentByIdAsync(appointmentId, kind, true, ct);
        if (state is null || state.Application.SchoolId != schoolId ||
            state.Lifecycle is not (AdmissionAppointmentLifecycle.Confirmed or
                AdmissionAppointmentLifecycle.InProgress) ||
            state.Mode != AdmissionAppointmentMode.Online || !state.MatchesWorkflow ||
            !await SchoolActorMayHostAsync(state.Application, actorUserId, ct))
            return null;
        var resolved = await ResolveReadyAsync(appointmentId, kind, state, true, ct);
        if (resolved is null || !resolved.Settings.SupportsHostAccess) return null;
        var now = DateTimeOffset.UtcNow;
        if (now < state.StartsAtUtc.AddMinutes(-resolved.Settings.HostBeforeMinutes) ||
            now > state.EndsAtUtc.AddMinutes(resolved.Settings.JoinAfterMinutes))
            return null;
        var action = await resolved.Provider.ResolveHostAccessAsync(resolved.Context, ct);
        if (action is null) return null;
        db.AdmissionMeetingSessionHistory.Add(new(
            resolved.Session.Id, MeetingSessionHistoryAction.HostAccessIssued,
            resolved.Session.Status, resolved.Session.Status, "meeting.hostAccess.issued",
            actorUserId, $"meeting:{resolved.Session.Id:N}:host:{Guid.NewGuid():N}"));
        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Issued safe School Host meeting access for session {MeetingSessionId}.",
            resolved.Session.Id);
        return action;
    }

    public async Task<SchoolMeetingSessionContextDto?> GetSchoolContextAsync(
        Guid schoolId, Guid actorUserId, Guid appointmentId, SlotKind kind,
        CancellationToken ct = default)
    {
        var state = await LoadAppointmentByIdAsync(appointmentId, kind, false, ct);
        if (state is null || state.Application.SchoolId != schoolId ||
            !await SchoolActorMayHostAsync(state.Application, actorUserId, ct))
            return null;
        var session = await db.AdmissionMeetingSessions.AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId && x.Kind == kind)
            .OrderByDescending(x => x.Generation).FirstOrDefaultAsync(ct);
        var summary = await GetSummaryAsync(appointmentId, kind, true, ct);
        if (summary is null) return null;
        return session is null
            ? new(Guid.Empty, appointmentId, kind, summary, [])
            : new(session.Id, appointmentId, kind, summary, session.RowVersion);
    }

    public async Task<IReadOnlyList<FailedMeetingSessionListItemDto>> ListFailedAsync(
        int take, CancellationToken ct = default) =>
        await db.AdmissionMeetingSessions.AsNoTracking()
            .Where(x => x.Status == MeetingSessionStatus.ProvisioningFailed ||
                (x.Status == MeetingSessionStatus.Ready && x.LastSafeFailureCode != null))
            .OrderByDescending(x => x.UpdatedAtUtc).Take(Math.Clamp(take, 1, 200))
            .Select(x => new FailedMeetingSessionListItemDto(
                x.Id, x.AppointmentId, x.AdmissionApplicationId, x.Kind, x.ProviderCode,
                x.Status, x.LastSafeFailureCode, x.ProvisioningAttemptCount,
                x.NextRetryAtUtc, x.UpdatedAtUtc, x.RowVersion)).ToListAsync(ct);

    public async Task<bool> RetryAsync(
        Guid meetingSessionId, byte[] rowVersion, string idempotencyKey, Guid actorUserId,
        CancellationToken ct = default)
    {
        var session = await db.AdmissionMeetingSessions
            .SingleOrDefaultAsync(x => x.Id == meetingSessionId, ct);
        if (session is null) return false;
        if (await db.AdmissionMeetingSessionHistory.AsNoTracking().AnyAsync(
                x => x.AdmissionMeetingSessionId == session.Id &&
                    x.IdempotencyKey == idempotencyKey, ct))
            return true;
        if (session.Status != MeetingSessionStatus.ProvisioningFailed ||
            !session.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return false;
        var old = session.Status;
        session.BeginProvisioningRetry();
        db.AdmissionMeetingSessionHistory.Add(new(
            session.Id, MeetingSessionHistoryAction.RetryRequested, old, session.Status,
            "meeting.retry.manual", actorUserId, NormalizeKey(idempotencyKey,
                $"meeting:{session.Id:N}:retry:{session.ProvisioningAttemptCount + 1}")));
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task ProcessOneAsync(Guid sessionId, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = {($"meeting-session:{sessionId:N}")},
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 5000;
                IF @result < 0 THROW 51000, 'Meeting session lock unavailable.', 1;
                """, ct);
            var session = await db.AdmissionMeetingSessions
                .SingleOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session is null || session.IsTerminal)
            {
                await transaction.CommitAsync(ct);
                return;
            }
            var state = await LoadAppointmentByIdAsync(session.AppointmentId, session.Kind, true, ct);
            if (state is null || state.Lifecycle is AdmissionAppointmentLifecycle.Cancelled or
                AdmissionAppointmentLifecycle.RescheduleRequested)
            {
                await HandleAppointmentUnavailableAsync(
                    session.AppointmentId, session.Kind, null,
                    $"meeting:{session.Id:N}:appointment-unavailable", ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            if (state.Lifecycle is AdmissionAppointmentLifecycle.Completed or
                AdmissionAppointmentLifecycle.NoShow)
            {
                await HandleAppointmentEndedAsync(
                    session.AppointmentId, session.Kind, null,
                    $"meeting:{session.Id:N}:appointment-ended", ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            if ((session.Status is MeetingSessionStatus.PendingProvisioning or
                    MeetingSessionStatus.ProvisioningFailed) &&
                state.Lifecycle != AdmissionAppointmentLifecycle.Confirmed)
            {
                await HandleAppointmentUnavailableAsync(
                    session.AppointmentId, session.Kind, null,
                    $"meeting:{session.Id:N}:not-confirmed", ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            if (session.Status == MeetingSessionStatus.Ready &&
                session.ExpirationAtUtc <= DateTimeOffset.UtcNow)
            {
                var old = session.Status;
                session.MarkExpired();
                db.AdmissionMeetingSessionHistory.Add(new(
                    session.Id, MeetingSessionHistoryAction.Expired, old, session.Status,
                    "meeting.expired", null, $"meeting:{session.Id:N}:expired"));
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            if (session.Status == MeetingSessionStatus.ProvisioningFailed)
                session.BeginProvisioningRetry();

            var cancellationOperation =
                session.Status == MeetingSessionStatus.CancellationPending ||
                session.CancellationRequestedAtUtc.HasValue &&
                session.Status == MeetingSessionStatus.Ready;
            var resolved = await ResolveProviderAsync(
                session, state, requireActive: !cancellationOperation, ct);
            if (resolved is null)
            {
                if (session.Status == MeetingSessionStatus.PendingProvisioning)
                    session.MarkProvisioningFailed("meeting.provider.unavailable", false, null);
                await SaveFailureHistoryAsync(session, "meeting.provider.unavailable", ct);
                await transaction.CommitAsync(ct);
                return;
            }

            if (cancellationOperation)
            {
                if (session.Status == MeetingSessionStatus.Ready)
                    session.RequestCancellation(true);
                await ProcessCancellationAsync(session, resolved, ct);
            }
            else if (session.Status == MeetingSessionStatus.PendingProvisioning)
                await ProcessProvisioningAsync(session, state, resolved, ct);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }

    private async Task ProcessProvisioningAsync(
        AdmissionMeetingSession session, AppointmentState state,
        ResolvedProvider resolved, CancellationToken ct)
    {
        var old = session.Status;
        session.RecordProvisioningAttempt();
        var result = await resolved.Provider.ProvisionAsync(resolved.Context, ct);
        if (result.Result == MeetingProviderOperationResult.Succeeded &&
            !string.IsNullOrWhiteSpace(result.SafeProviderMeetingReference))
        {
            session.MarkReady(result.SafeProviderMeetingReference, result.SafeCode);
            resolved.Integration.RecordSuccessfulUse();
            db.AdmissionMeetingSessionHistory.Add(new(
                session.Id, MeetingSessionHistoryAction.ProvisioningSucceeded, old, session.Status,
                result.SafeCode, null,
                $"meeting:{session.Id:N}:provision:{session.ProvisioningAttemptCount}"));
            await EnqueueOperationalAsync(
                state.Application, NotificationEventType.OnlineMeetingReady, session, ct);
            return;
        }
        if (result.Result == MeetingProviderOperationResult.Pending &&
            session.ProvisioningAttemptCount < resolved.Settings.MaxRetryAttempts)
        {
            session.KeepProvisioningPending(
                result.SafeCode,
                DateTimeOffset.UtcNow.AddSeconds(resolved.Settings.RetryDelaySeconds));
            return;
        }
        var canRetry = (result.Result is MeetingProviderOperationResult.Pending or
            MeetingProviderOperationResult.RetryableFailure) &&
            session.ProvisioningAttemptCount < resolved.Settings.MaxRetryAttempts;
        DateTimeOffset? nextRetry = canRetry
            ? DateTimeOffset.UtcNow.AddSeconds(resolved.Settings.RetryDelaySeconds)
            : null;
        session.MarkProvisioningFailed(result.SafeCode, canRetry, nextRetry);
        resolved.Integration.RecordHealth(IntegrationHealthStatus.Degraded, result.SafeCode);
        db.AdmissionMeetingSessionHistory.Add(new(
            session.Id, MeetingSessionHistoryAction.ProvisioningFailed, old, session.Status,
            result.SafeCode, null,
            $"meeting:{session.Id:N}:provision:{session.ProvisioningAttemptCount}"));
        if (!canRetry)
            await EnqueueOperationalAsync(
                state.Application, NotificationEventType.OnlineMeetingProvisioningFailed,
                session, ct);
    }

    private async Task ProcessCancellationAsync(
        AdmissionMeetingSession session, ResolvedProvider resolved, CancellationToken ct)
    {
        session.RecordCancellationAttempt();
        if (string.IsNullOrWhiteSpace(session.ProviderMeetingReference) ||
            !resolved.Settings.SupportsCancellation)
        {
            var old = session.Status;
            session.RequestCancellation(false);
            db.AdmissionMeetingSessionHistory.Add(new(
                session.Id, MeetingSessionHistoryAction.CancellationSucceeded, old, session.Status,
                "meeting.cancelled.locally", null, $"meeting:{session.Id:N}:cancelled"));
            return;
        }
        var result = await resolved.Provider.CancelAsync(
            resolved.Context, session.ProviderMeetingReference, ct);
        var previous = session.Status;
        if (result.Result == MeetingProviderOperationResult.Succeeded)
        {
            session.MarkCancellationSucceeded(result.SafeCode);
            db.AdmissionMeetingSessionHistory.Add(new(
                session.Id, MeetingSessionHistoryAction.CancellationSucceeded,
                previous, session.Status, result.SafeCode, null,
                $"meeting:{session.Id:N}:cancel:{session.CancellationAttemptCount}"));
        }
        else
        {
            session.MarkCancellationFailed(
                result.SafeCode,
                result.Result == MeetingProviderOperationResult.RetryableFailure
                    ? DateTimeOffset.UtcNow.AddSeconds(resolved.Settings.RetryDelaySeconds)
                    : null);
            db.AdmissionMeetingSessionHistory.Add(new(
                session.Id, MeetingSessionHistoryAction.CancellationFailed,
                previous, session.Status, result.SafeCode, null,
                $"meeting:{session.Id:N}:cancel-failed:{session.CancellationAttemptCount}"));
        }
    }

    private async Task SaveFailureHistoryAsync(
        AdmissionMeetingSession session, string code, CancellationToken ct)
    {
        db.AdmissionMeetingSessionHistory.Add(new(
            session.Id, MeetingSessionHistoryAction.ProvisioningFailed,
            MeetingSessionStatus.PendingProvisioning, session.Status, code, null,
            $"meeting:{session.Id:N}:provider-unavailable"));
        await db.SaveChangesAsync(ct);
    }

    private async Task<ResolvedAccess?> ResolveReadyAsync(
        Guid appointmentId, SlotKind kind, AppointmentState state, bool school,
        CancellationToken ct)
    {
        var session = await db.AdmissionMeetingSessions
            .Where(x => x.AppointmentId == appointmentId && x.Kind == kind &&
                x.Status == MeetingSessionStatus.Ready)
            .OrderByDescending(x => x.Generation).FirstOrDefaultAsync(ct);
        if (session is null || session.ExpirationAtUtc <= DateTimeOffset.UtcNow ||
            session.ScheduledStartAtUtc != state.StartsAtUtc ||
            session.ScheduledEndAtUtc != state.EndsAtUtc)
            return null;
        var provider = await ResolveProviderAsync(session, state, requireActive: true, ct);
        return provider is null ? null : new(
            session, provider.Provider, provider.Settings, provider.Context);
    }

    private async Task<ResolvedProvider?> ResolveProviderAsync(
        AdmissionMeetingSession session, AppointmentState state, bool requireActive,
        CancellationToken ct)
    {
        var integration = await db.PlatformIntegrationConfigurations.SingleOrDefaultAsync(
            x => x.Id == session.IntegrationConfigurationId &&
                x.IntegrationType == IntegrationType.Meeting &&
                (!requireActive || x.IsActive), ct);
        if (integration is null || !TrySettings(integration, out var settings, out var environment) ||
            environment != session.ProviderEnvironment || !RuntimeAllows(environment) ||
            requireActive && integration.HealthStatus == IntegrationHealthStatus.Unhealthy)
            return null;
        var provider = meetingProviders.FirstOrDefault(x =>
            string.Equals(x.ProviderCode, integration.ProviderCode, StringComparison.OrdinalIgnoreCase) ||
            x.IsSimulated && IntegrationProviderCodes.IsSimulated(integration.ProviderCode));
        if (provider is null) return null;
        return new(integration, provider, settings, new(
            session.Id, session.AppointmentId, session.ProvisioningIdempotencyKey,
            state.StartsAtUtc, state.EndsAtUtc, settings));
    }

    private async Task<MeetingIntegrationSettings?> GetHistoricalSettingsAsync(
        AdmissionMeetingSession session, bool requireActive, CancellationToken ct)
    {
        var integration = await db.PlatformIntegrationConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == session.IntegrationConfigurationId &&
                (!requireActive || x.IsActive), ct);
        return integration is not null && TrySettings(integration, out var settings, out _)
            ? settings : null;
    }

    private bool TrySettings(
        PlatformIntegrationConfiguration integration,
        out MeetingIntegrationSettings settings,
        out MeetingProviderEnvironment environment)
    {
        settings = new();
        environment = default;
        var validation = settingsValidator.Validate(
            integration.IntegrationType, integration.ProviderCode,
            integration.SettingsJson, integration.SettingsSchemaVersion);
        if (!validation.IsValid) return false;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<MeetingIntegrationSettings>(
                integration.SettingsJson);
            return Enum.TryParse(settings.Environment, true, out environment);
        }
        catch { return false; }
    }

    private bool RuntimeAllows(MeetingProviderEnvironment environment) =>
        environment switch
        {
            MeetingProviderEnvironment.Development => runtimeEnvironment.IsDevelopment,
            MeetingProviderEnvironment.Test => runtimeEnvironment.IsEnvironment("Test"),
            _ => false,
        };

    private MeetingSessionSummaryDto BuildSummary(
        AdmissionMeetingSession session, AppointmentState state,
        PlatformIntegrationConfiguration? integration,
        MeetingIntegrationSettings? settings,
        bool forSchool)
    {
        var now = DateTimeOffset.UtcNow;
        var active = integration?.IsActive == true &&
            integration.HealthStatus != IntegrationHealthStatus.Unhealthy && settings is not null &&
            RuntimeAllows(session.ProviderEnvironment);
        var parentStart = settings is null ? (DateTimeOffset?)null
            : state.StartsAtUtc.AddMinutes(-settings.JoinBeforeMinutes);
        var parentEnd = settings is null ? (DateTimeOffset?)null
            : state.EndsAtUtc.AddMinutes(settings.JoinAfterMinutes);
        var hostStart = settings is null ? (DateTimeOffset?)null
            : state.StartsAtUtc.AddMinutes(-settings.HostBeforeMinutes);
        var parentAllowed = active && settings!.SupportsParentAccess &&
            session.Status == MeetingSessionStatus.Ready &&
            state.Lifecycle == AdmissionAppointmentLifecycle.Confirmed &&
            state.Mode == AdmissionAppointmentMode.Online && state.MatchesWorkflow &&
            now >= parentStart && now <= parentEnd;
        var hostAllowed = forSchool && active && settings!.SupportsHostAccess &&
            session.Status == MeetingSessionStatus.Ready &&
            (state.Lifecycle is AdmissionAppointmentLifecycle.Confirmed or
                AdmissionAppointmentLifecycle.InProgress) &&
            now >= hostStart && now <= parentEnd;
        var reason = parentAllowed ? null :
            !active ? "meeting.provider.disabled" :
            session.Status == MeetingSessionStatus.PendingProvisioning ? "meeting.pending" :
            session.Status == MeetingSessionStatus.ProvisioningFailed ? "meeting.failed" :
            session.Status == MeetingSessionStatus.Cancelled ? "meeting.cancelled" :
            session.Status == MeetingSessionStatus.Expired ? "meeting.expired" :
            now < parentStart ? "meeting.join.tooEarly" :
            now > parentEnd ? "meeting.join.expired" :
            "meeting.join.unavailable";
        return new(
            session.Status, session.LastSafeFailureCode, session.ProvisionedAtUtc,
            session.ExpirationAtUtc,
            integration is null || settings is null ? null : new(
                integration.ProviderCode, settings.PublicNameAr, settings.PublicNameEn,
                settings.Environment, integration.IsActive, integration.HealthStatus,
                settings.SupportsMeetingCreation, settings.SupportsParentAccess,
                settings.SupportsHostAccess, settings.SupportsCancellation,
                settings.SupportsStatusQuery, settings.TermsUrl, settings.PrivacyUrl,
                IntegrationProviderCodes.IsSimulated(integration.ProviderCode)),
            integration is not null && IntegrationProviderCodes.IsSimulated(integration.ProviderCode),
            parentAllowed, hostAllowed,
            forSchool && session.Status == MeetingSessionStatus.ProvisioningFailed &&
                session.NextRetryAtUtc is null,
            forSchool && (!active || session.Status == MeetingSessionStatus.ProvisioningFailed),
            reason, parentStart, parentEnd, hostStart);
    }

    private async Task<AppointmentState?> LoadAppointmentAsync(
        Guid applicationId, Guid appointmentId, SlotKind kind, bool tracking, CancellationToken ct)
    {
        var application = await ApplicationQuery(tracking)
            .SingleOrDefaultAsync(x => x.Id == applicationId, ct);
        return application is null ? null : AppointmentState.For(application, appointmentId, kind);
    }

    private async Task<AppointmentState?> LoadAppointmentByIdAsync(
        Guid appointmentId, SlotKind kind, bool tracking, CancellationToken ct)
    {
        var applicationId = kind == SlotKind.Interview
            ? await db.AdmissionInterviewAppointments.AsNoTracking()
                .Where(x => x.Id == appointmentId)
                .Select(x => (Guid?)x.AdmissionApplicationId).SingleOrDefaultAsync(ct)
            : await db.AdmissionAssessmentAppointments.AsNoTracking()
                .Where(x => x.Id == appointmentId)
                .Select(x => (Guid?)x.AdmissionApplicationId).SingleOrDefaultAsync(ct);
        return applicationId.HasValue
            ? await LoadAppointmentAsync(applicationId.Value, appointmentId, kind, tracking, ct)
            : null;
    }

    private IQueryable<AdmissionApplication> ApplicationQuery(bool tracking)
    {
        var query = db.AdmissionApplications
            .Include(x => x.PolicySnapshot)
            .Include(x => x.InterviewAppointments)
            .Include(x => x.AssessmentAppointments);
        return tracking ? query : query.AsNoTracking();
    }

    private async Task<bool> SchoolActorMayHostAsync(
        AdmissionApplication application, Guid actorUserId, CancellationToken ct)
    {
        var owner = await db.Schools.AsNoTracking()
            .AnyAsync(x => x.Id == application.SchoolId && x.OwnerUserId == actorUserId, ct);
        if (owner) return true;
        return await db.SchoolTeamMembers.AsNoTracking().AnyAsync(x =>
            x.SchoolId == application.SchoolId && x.UserId == actorUserId && x.IsActive &&
            (x.Role == SchoolTeamRole.SchoolAdmin || x.Role == SchoolTeamRole.AdmissionOfficer) &&
            (x.BranchScopeMode == SchoolBranchScopeMode.AllBranches ||
             x.BranchAssignments.Any(b =>
                 b.SchoolBranchId == application.SchoolBranchId)), ct);
    }

    private async Task EnqueueOperationalAsync(
        AdmissionApplication application, NotificationEventType eventType,
        AdmissionMeetingSession session, CancellationToken ct)
    {
        var culture = await db.Users.AsNoTracking().Where(x => x.Id == application.ParentUserId)
            .Select(x => x.PreferredLanguage).SingleOrDefaultAsync(ct);
        await notificationOutbox.EnqueueAsync(new(
            application.ParentUserId, eventType,
            culture?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "ar",
            $"meeting:{session.Id:N}:status:{(int)session.Status}",
            new Dictionary<string, string>
            {
                ["applicationNumber"] = application.ApplicationNumber,
                ["providerName"] = session.ProviderCode,
            },
            $"/parent/applications/{application.Id}", application.SchoolId, application.Id,
            [NotificationChannel.InApp], SkipPreferenceCheck: true), ct);
    }

    private static string NormalizeKey(string value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized[..Math.Min(128, normalized.Length)];
    }

    private sealed record ResolvedProvider(
        PlatformIntegrationConfiguration Integration,
        IMeetingProvider Provider,
        MeetingIntegrationSettings Settings,
        MeetingProvisioningContext Context);

    private sealed record ResolvedAccess(
        AdmissionMeetingSession Session,
        IMeetingProvider Provider,
        MeetingIntegrationSettings Settings,
        MeetingProvisioningContext Context);

    private sealed class AppointmentState
    {
        private readonly AdmissionInterviewAppointment? interview;
        private readonly AdmissionAssessmentAppointment? assessment;
        private AppointmentState(AdmissionApplication application, AdmissionInterviewAppointment value)
        {
            Application = application;
            interview = value;
        }
        private AppointmentState(AdmissionApplication application, AdmissionAssessmentAppointment value)
        {
            Application = application;
            assessment = value;
        }
        public AdmissionApplication Application { get; }
        public DateTimeOffset StartsAtUtc => interview?.ScheduledAtUtc ?? assessment!.ScheduledAtUtc;
        public DateTimeOffset EndsAtUtc => StartsAtUtc.AddMinutes(
            Application.PolicySnapshot?.ExpectedDurationMinutes ?? 0);
        public AdmissionAppointmentMode Mode => interview?.Mode ?? assessment!.Mode;
        public AdmissionAppointmentLifecycle Lifecycle => interview?.Lifecycle ?? assessment!.Lifecycle;
        public bool MatchesWorkflow =>
            interview is not null
                ? Application.Status == AdmissionApplicationStatus.InterviewRequired
                : Application.Status == AdmissionApplicationStatus.AssessmentRequired;
        public static AppointmentState? For(
            AdmissionApplication application, Guid appointmentId, SlotKind kind) =>
            kind == SlotKind.Interview
                ? application.InterviewAppointments.FirstOrDefault(x => x.Id == appointmentId) is { } interview
                    ? new(application, interview) : null
                : application.AssessmentAppointments.FirstOrDefault(x => x.Id == appointmentId) is { } assessment
                    ? new(application, assessment) : null;
    }
}
