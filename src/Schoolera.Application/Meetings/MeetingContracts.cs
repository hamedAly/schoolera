using Schoolera.Application.Integrations;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Meetings;

public sealed record SafeMeetingProviderMetadataDto(
    string ProviderCode,
    string PublicNameAr,
    string PublicNameEn,
    string Environment,
    bool IsActive,
    IntegrationHealthStatus HealthStatus,
    bool SupportsMeetingCreation,
    bool SupportsParentAccess,
    bool SupportsHostAccess,
    bool SupportsCancellation,
    bool SupportsStatusQuery,
    string? TermsUrl,
    string? PrivacyUrl,
    bool IsSimulated);

public sealed record OnSiteAttendanceDetailsDto(
    string BranchNameAr,
    string? BranchNameEn,
    string? PublicAddressAr,
    string? PublicAddressEn,
    string? SafeArrivalDirections,
    string? AttendanceInstructions,
    string? PreparationInstructions,
    InterviewAssessmentRequiredParticipants? RequiredParticipants,
    DateTimeOffset ScheduledAtUtc,
    string TimeZoneId,
    DateTimeOffset? ArrivalWindowStartsAtUtc,
    bool BranchIsActive,
    bool RequiresSchoolCorrection);

public sealed record MeetingSessionSummaryDto(
    MeetingSessionStatus Status,
    string? SafeFailureCode,
    DateTimeOffset? ProvisionedAtUtc,
    DateTimeOffset ExpirationAtUtc,
    SafeMeetingProviderMetadataDto? Provider,
    bool IsSimulated,
    bool CanParentJoin,
    bool CanSchoolHost,
    bool CanRetry,
    bool RequiresSchoolCorrection,
    string? JoinUnavailableReasonCode,
    DateTimeOffset? ParentJoinWindowStartsAtUtc,
    DateTimeOffset? ParentJoinWindowEndsAtUtc,
    DateTimeOffset? HostWindowStartsAtUtc);

public sealed record MeetingAccessActionDto(
    string ActionPath,
    DateTimeOffset ExpiresAtUtc,
    bool IsSimulated,
    string Role);

public sealed record SchoolMeetingSessionContextDto(
    Guid MeetingSessionId,
    Guid AppointmentId,
    SlotKind Kind,
    MeetingSessionSummaryDto Summary,
    byte[] RowVersion);

public sealed record FailedMeetingSessionListItemDto(
    Guid MeetingSessionId,
    Guid AppointmentId,
    Guid AdmissionApplicationId,
    SlotKind Kind,
    string ProviderCode,
    MeetingSessionStatus Status,
    string? SafeFailureCode,
    int AttemptCount,
    DateTimeOffset? NextRetryAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion);

public sealed record RetryMeetingSessionRequest(
    byte[] RowVersion,
    string IdempotencyKey);

public enum MeetingProviderOperationResult
{
    Succeeded = 1,
    Pending = 2,
    RetryableFailure = 3,
    PermanentFailure = 4,
}

public sealed record MeetingProvisioningContext(
    Guid MeetingSessionId,
    Guid AppointmentId,
    string IdempotencyKey,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    MeetingIntegrationSettings Settings);

public sealed record MeetingProviderResult(
    MeetingProviderOperationResult Result,
    string SafeCode,
    string? SafeProviderMeetingReference = null);

public interface IMeetingProvider
{
    string ProviderCode { get; }
    bool IsSimulated { get; }
    Task<MeetingProviderResult> ProvisionAsync(
        MeetingProvisioningContext context, CancellationToken cancellationToken = default);
    Task<MeetingProviderResult> CancelAsync(
        MeetingProvisioningContext context, string safeProviderMeetingReference,
        CancellationToken cancellationToken = default);
    Task<MeetingAccessActionDto?> ResolveParentAccessAsync(
        MeetingProvisioningContext context, CancellationToken cancellationToken = default);
    Task<MeetingAccessActionDto?> ResolveHostAccessAsync(
        MeetingProvisioningContext context, CancellationToken cancellationToken = default);
}

public interface IRuntimeEnvironment
{
    bool IsDevelopment { get; }
    bool IsEnvironment(string environmentName);
}

public interface IMeetingSessionService
{
    Task EnsureForConfirmedAppointmentAsync(
        Guid admissionApplicationId, Guid appointmentId, SlotKind kind,
        CancellationToken cancellationToken = default);
    Task HandleAppointmentUnavailableAsync(
        Guid appointmentId, SlotKind kind, Guid? actorUserId, string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task HandleAppointmentEndedAsync(
        Guid appointmentId, SlotKind kind, Guid? actorUserId, string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task ProcessDueSessionsAsync(CancellationToken cancellationToken = default);
    Task<MeetingSessionSummaryDto?> GetSummaryAsync(
        Guid appointmentId, SlotKind kind, bool forSchool,
        CancellationToken cancellationToken = default);
    Task<MeetingAccessActionDto?> ResolveParentAccessAsync(
        Guid parentUserId, Guid applicationId, Guid appointmentId, SlotKind kind,
        CancellationToken cancellationToken = default);
    Task<MeetingAccessActionDto?> ResolveSchoolHostAccessAsync(
        Guid schoolId, Guid actorUserId, Guid appointmentId, SlotKind kind,
        CancellationToken cancellationToken = default);
    Task<SchoolMeetingSessionContextDto?> GetSchoolContextAsync(
        Guid schoolId, Guid actorUserId, Guid appointmentId, SlotKind kind,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FailedMeetingSessionListItemDto>> ListFailedAsync(
        int take, CancellationToken cancellationToken = default);
    Task<bool> RetryAsync(
        Guid meetingSessionId, byte[] rowVersion, string idempotencyKey, Guid actorUserId,
        CancellationToken cancellationToken = default);
}
