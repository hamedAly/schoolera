using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Dtos;

/// <summary>Owner-visible status-history entry. Internal admin notes are never included.</summary>
public sealed record OnboardingStatusHistoryDto(
    string? PreviousStatus,
    string NewStatus,
    DateTimeOffset CreatedAtUtc,
    string? OwnerVisibleReason)
{
    public static OnboardingStatusHistoryDto FromEntity(SchoolOnboardingStatusHistory history) =>
        new(
            history.PreviousStatus?.ToString(),
            history.NewStatus.ToString(),
            history.CreatedAtUtc,
            history.OwnerVisibleReason);
}

/// <summary>Admin-visible status-history entry, including the internal note and actor.</summary>
public sealed record AdminOnboardingStatusHistoryDto(
    string? PreviousStatus,
    string NewStatus,
    Guid ActorUserId,
    DateTimeOffset CreatedAtUtc,
    string? OwnerVisibleReason,
    string? InternalNote)
{
    public static AdminOnboardingStatusHistoryDto FromEntity(SchoolOnboardingStatusHistory history) =>
        new(
            history.PreviousStatus?.ToString(),
            history.NewStatus.ToString(),
            history.ActorUserId,
            history.CreatedAtUtc,
            history.OwnerVisibleReason,
            history.InternalNote);
}
