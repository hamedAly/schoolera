using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class AdmissionMeetingSession
{
    private AdmissionMeetingSession() { }

    public AdmissionMeetingSession(
        Guid admissionApplicationId,
        Guid appointmentId,
        SlotKind kind,
        Guid schoolId,
        Guid schoolBranchId,
        Guid integrationConfigurationId,
        string providerCode,
        MeetingProviderEnvironment providerEnvironment,
        int settingsSchemaVersion,
        byte[] configurationRowVersion,
        int generation,
        string provisioningIdempotencyKey,
        DateTimeOffset scheduledStartAtUtc,
        DateTimeOffset scheduledEndAtUtc,
        DateTimeOffset expirationAtUtc)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        AppointmentId = appointmentId;
        Kind = kind;
        SchoolId = schoolId;
        SchoolBranchId = schoolBranchId;
        IntegrationConfigurationId = integrationConfigurationId;
        ProviderCode = Required(providerCode, 100);
        ProviderEnvironment = providerEnvironment;
        SettingsSchemaVersion = settingsSchemaVersion;
        ConfigurationRowVersion = configurationRowVersion.ToArray();
        Generation = generation;
        ProvisioningIdempotencyKey = Required(provisioningIdempotencyKey, 128);
        ScheduledStartAtUtc = scheduledStartAtUtc;
        ScheduledEndAtUtc = scheduledEndAtUtc;
        Status = MeetingSessionStatus.PendingProvisioning;
        ExpirationAtUtc = expirationAtUtc;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionApplicationId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public SlotKind Kind { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid SchoolBranchId { get; private set; }
    public Guid IntegrationConfigurationId { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public MeetingProviderEnvironment ProviderEnvironment { get; private set; }
    public int SettingsSchemaVersion { get; private set; }
    public byte[] ConfigurationRowVersion { get; private set; } = [];
    public int Generation { get; private set; }
    public string ProvisioningIdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledStartAtUtc { get; private set; }
    public DateTimeOffset ScheduledEndAtUtc { get; private set; }
    public MeetingSessionStatus Status { get; private set; }
    public string? ProviderMeetingReference { get; private set; }
    public int ProvisioningAttemptCount { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset? NextRetryAtUtc { get; private set; }
    public DateTimeOffset? ProvisionedAtUtc { get; private set; }
    public DateTimeOffset? CancellationRequestedAtUtc { get; private set; }
    public int CancellationAttemptCount { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset ExpirationAtUtc { get; private set; }
    public string? LastSafeProviderStatusCode { get; private set; }
    public string? LastSafeFailureCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsTerminal => Status is MeetingSessionStatus.Cancelled or MeetingSessionStatus.Expired;

    public void BeginProvisioningRetry()
    {
        Ensure(MeetingSessionStatus.ProvisioningFailed);
        Status = MeetingSessionStatus.PendingProvisioning;
        NextRetryAtUtc = null;
        LastSafeFailureCode = null;
        Touch();
    }

    public void RecordProvisioningAttempt()
    {
        Ensure(MeetingSessionStatus.PendingProvisioning);
        ProvisioningAttemptCount++;
        LastAttemptAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkReady(string providerMeetingReference, string safeProviderStatusCode)
    {
        Ensure(MeetingSessionStatus.PendingProvisioning);
        ProviderMeetingReference = Required(providerMeetingReference, 200);
        LastSafeProviderStatusCode = Required(safeProviderStatusCode, 100);
        LastSafeFailureCode = null;
        NextRetryAtUtc = null;
        ProvisionedAtUtc = DateTimeOffset.UtcNow;
        Status = MeetingSessionStatus.Ready;
        Touch();
    }

    public void MarkProvisioningFailed(
        string safeFailureCode,
        bool retryable,
        DateTimeOffset? nextRetryAtUtc)
    {
        Ensure(MeetingSessionStatus.PendingProvisioning);
        LastSafeFailureCode = Required(safeFailureCode, 100);
        LastSafeProviderStatusCode = null;
        NextRetryAtUtc = retryable ? nextRetryAtUtc : null;
        Status = MeetingSessionStatus.ProvisioningFailed;
        Touch();
    }

    public void KeepProvisioningPending(string safeProviderStatusCode, DateTimeOffset nextRetryAtUtc)
    {
        Ensure(MeetingSessionStatus.PendingProvisioning);
        LastSafeProviderStatusCode = Required(safeProviderStatusCode, 100);
        LastSafeFailureCode = null;
        NextRetryAtUtc = nextRetryAtUtc;
        Touch();
    }

    public void RequestCancellation(bool providerCancellationRequired)
    {
        if (IsTerminal) return;
        CancellationRequestedAtUtc ??= DateTimeOffset.UtcNow;
        if (Status is MeetingSessionStatus.PendingProvisioning or MeetingSessionStatus.ProvisioningFailed ||
            !providerCancellationRequired)
        {
            Status = MeetingSessionStatus.Cancelled;
            CancelledAtUtc = DateTimeOffset.UtcNow;
            NextRetryAtUtc = null;
        }
        else if (Status == MeetingSessionStatus.Ready)
        {
            Status = MeetingSessionStatus.CancellationPending;
        }
        Touch();
    }

    public void MarkCancellationSucceeded(string safeProviderStatusCode)
    {
        if (Status != MeetingSessionStatus.CancellationPending) return;
        Status = MeetingSessionStatus.Cancelled;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        LastSafeProviderStatusCode = Required(safeProviderStatusCode, 100);
        LastSafeFailureCode = null;
        NextRetryAtUtc = null;
        Touch();
    }

    public void RecordCancellationAttempt()
    {
        Ensure(MeetingSessionStatus.CancellationPending);
        CancellationAttemptCount++;
        LastAttemptAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkCancellationFailed(string safeFailureCode, DateTimeOffset? nextRetryAtUtc)
    {
        Ensure(MeetingSessionStatus.CancellationPending);
        Status = MeetingSessionStatus.Ready;
        LastSafeFailureCode = Required(safeFailureCode, 100);
        NextRetryAtUtc = nextRetryAtUtc;
        Touch();
    }

    public void MarkExpired()
    {
        if (IsTerminal) return;
        if (Status != MeetingSessionStatus.Ready)
            throw new InvalidOperationException("Only a ready session may expire.");
        Status = MeetingSessionStatus.Expired;
        NextRetryAtUtc = null;
        Touch();
    }

    private void Ensure(MeetingSessionStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Meeting session transition from {Status} is not allowed.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string Required(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.");
        var normalized = value.Trim();
        return normalized[..Math.Min(maxLength, normalized.Length)];
    }
}

public sealed class AdmissionMeetingSessionHistory
{
    private AdmissionMeetingSessionHistory() { }

    public AdmissionMeetingSessionHistory(
        Guid meetingSessionId,
        MeetingSessionHistoryAction action,
        MeetingSessionStatus previousStatus,
        MeetingSessionStatus newStatus,
        string? safeCode,
        Guid? actorUserId,
        string idempotencyKey)
    {
        Id = Guid.NewGuid();
        AdmissionMeetingSessionId = meetingSessionId;
        Action = action;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        SafeCode = Optional(safeCode, 100);
        ActorUserId = actorUserId;
        IdempotencyKey = Required(idempotencyKey, 128);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionMeetingSessionId { get; private set; }
    public MeetingSessionHistoryAction Action { get; private set; }
    public MeetingSessionStatus PreviousStatus { get; private set; }
    public MeetingSessionStatus NewStatus { get; private set; }
    public string? SafeCode { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private static string Required(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.");
        var normalized = value.Trim();
        return normalized[..Math.Min(maxLength, normalized.Length)];
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized[..Math.Min(maxLength, normalized.Length)];
    }
}
