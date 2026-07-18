using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Dtos;

public sealed record AdminOnboardingListItemDto(
    Guid Id,
    string Status,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string OwnerEmail,
    string? OrganizationNameAr,
    string? SchoolNameAr,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc);

/// <summary>
/// Full PlatformAdmin review projection. Reuses the shared owner-facing application shape and
/// adds admin-only data (owner identity, reviewer, and internal-note status history).
/// </summary>
public sealed record AdminOnboardingDetailDto(
    MyOnboardingApplicationDto Application,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string OwnerEmail,
    Guid? ReviewedByUserId,
    IReadOnlyList<AdminOnboardingStatusHistoryDto> AdminStatusHistory)
{
    public static AdminOnboardingDetailDto Create(
        SchoolOnboardingApplication application,
        MyOnboardingApplicationDto sharedApplication,
        UserSummary owner)
    {
        var adminHistory = application.StatusHistory
            .OrderBy(h => h.CreatedAtUtc)
            .Select(AdminOnboardingStatusHistoryDto.FromEntity)
            .ToArray();

        return new AdminOnboardingDetailDto(
            sharedApplication,
            application.OwnerUserId,
            owner.DisplayName,
            owner.Email,
            application.ReviewedByUserId,
            adminHistory);
    }
}
