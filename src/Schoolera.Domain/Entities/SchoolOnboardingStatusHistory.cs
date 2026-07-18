using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Append-only audit record of a single onboarding status transition.
/// <see cref="OwnerVisibleReason"/> may be shown to the owner; <see cref="InternalNote"/>
/// is PlatformAdmin-only and must never be exposed through owner-facing endpoints.
/// </summary>
public sealed class SchoolOnboardingStatusHistory
{
    private SchoolOnboardingStatusHistory()
    {
    }

    public SchoolOnboardingStatusHistory(
        Guid applicationId,
        SchoolOnboardingStatus? previousStatus,
        SchoolOnboardingStatus newStatus,
        Guid actorUserId,
        string? ownerVisibleReason,
        string? internalNote)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ActorUserId = actorUserId;
        OwnerVisibleReason = string.IsNullOrWhiteSpace(ownerVisibleReason) ? null : ownerVisibleReason.Trim();
        InternalNote = string.IsNullOrWhiteSpace(internalNote) ? null : internalNote.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ApplicationId { get; private set; }

    public SchoolOnboardingApplication Application { get; private set; } = null!;

    public SchoolOnboardingStatus? PreviousStatus { get; private set; }

    public SchoolOnboardingStatus NewStatus { get; private set; }

    public Guid ActorUserId { get; private set; }

    public string? OwnerVisibleReason { get; private set; }

    public string? InternalNote { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
