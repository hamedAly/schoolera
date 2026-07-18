using Schoolera.Application.Notifications.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Notifications.Common;

public static class ParentNotificationMapping
{
    public static ParentInAppNotificationDto ToInAppDto(NotificationOutboxMessage message) =>
        new(
            message.Id,
            message.EventType,
            message.Subject,
            message.BodyOrPayload,
            message.ActionPath,
            message.CreatedAtUtc,
            message.ReadAtUtc,
            message.ReadAtUtc is not null,
            message.RelatedSchoolId,
            message.RelatedEntityId);

    public static ParentNotificationPreferenceDto ToPreferenceDto(ParentNotificationPreference preference) =>
        new(
            preference.Id,
            preference.InAppEnabled,
            preference.EmailEnabled,
            preference.SmsEnabled,
            preference.WhatsAppEnabled,
            preference.OptionalAdmissionsOpenEnabled,
            preference.EmailConsentAtUtc,
            preference.SmsConsentAtUtc,
            preference.WhatsAppConsentAtUtc,
            preference.UpdatedAtUtc,
            preference.RowVersion);

    public static ParentAdmissionOpenSubscriptionDto ToSubscriptionDto(
        ParentAdmissionOpenSubscription subscription) =>
        new(
            subscription.Id,
            subscription.SchoolId,
            subscription.SchoolBranchId,
            subscription.EducationalStageId,
            subscription.GradeId,
            subscription.AcademicYearId,
            subscription.PreferredChannel,
            subscription.IsActive,
            subscription.CreatedAtUtc,
            subscription.UnsubscribedAtUtc);

    public static bool HasRowVersionMismatch(byte[]? requested, byte[] current) =>
        requested is { Length: > 0 } &&
        (current.Length == 0 || !requested.SequenceEqual(current));
}
