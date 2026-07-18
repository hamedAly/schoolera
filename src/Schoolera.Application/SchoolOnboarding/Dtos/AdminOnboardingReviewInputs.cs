namespace Schoolera.Application.SchoolOnboarding.Dtos;

/// <summary>Request body for a review decision that requires an owner-visible reason.</summary>
public sealed record OnboardingReviewReasonInput(string OwnerVisibleReason, string? InternalNote);

/// <summary>Request body for approval, which allows only an optional internal note.</summary>
public sealed record OnboardingApprovalInput(string? InternalNote);
