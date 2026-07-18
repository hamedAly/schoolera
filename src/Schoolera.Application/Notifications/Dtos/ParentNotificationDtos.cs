using Schoolera.Domain.Enums;

namespace Schoolera.Application.Notifications.Dtos;

public sealed record ParentInAppNotificationDto(
    Guid Id,
    NotificationEventType EventType,
    string? Subject,
    string Body,
    string? ActionPath,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc,
    bool IsRead,
    Guid? RelatedSchoolId,
    Guid? RelatedEntityId);

public sealed record ParentNotificationPreferenceDto(
    Guid Id,
    bool InAppEnabled,
    bool EmailEnabled,
    bool SmsEnabled,
    bool WhatsAppEnabled,
    bool OptionalAdmissionsOpenEnabled,
    DateTimeOffset? EmailConsentAtUtc,
    DateTimeOffset? SmsConsentAtUtc,
    DateTimeOffset? WhatsAppConsentAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion);

public sealed record UpdateParentNotificationPreferencesRequest(
    bool InAppEnabled,
    bool EmailEnabled,
    bool SmsEnabled,
    bool WhatsAppEnabled,
    bool OptionalAdmissionsOpenEnabled,
    string? ConsentSource,
    byte[]? RowVersion);

public sealed record ParentAdmissionOpenSubscriptionDto(
    Guid Id,
    Guid SchoolId,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    NotificationChannel PreferredChannel,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UnsubscribedAtUtc);

public sealed record CreateParentAdmissionOpenSubscriptionRequest(
    Guid SchoolId,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    NotificationChannel PreferredChannel);

public sealed record ChannelAvailabilityDto(
    bool InAppAvailable,
    bool EmailConfigured,
    bool SmsConfigured,
    bool WhatsAppConfigured);
