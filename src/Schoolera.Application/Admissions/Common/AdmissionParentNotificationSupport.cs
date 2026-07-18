using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Enqueues parent admission lifecycle notifications with stable dedup keys.</summary>
public static class AdmissionParentNotificationSupport
{
    public static async Task EnqueueAsync(
        INotificationOutboxPublisher publisher,
        IParentAccountService parentAccountService,
        ISchoolPortalRepository schoolPortalRepository,
        AdmissionApplication application,
        NotificationEventType eventType,
        string actionKey,
        CancellationToken cancellationToken)
    {
        var school = await schoolPortalRepository.GetSchoolProfileAsync(
            application.SchoolId,
            cancellationToken);
        var schoolName = school?.NameAr
            ?? school?.NameEn
            ?? application.SchoolId.ToString();

        var account = await parentAccountService.GetAsync(application.ParentUserId, cancellationToken);
        var culture = NormalizeCulture(account?.PreferredLanguage);
        var status = application.Status.ToString();

        await publisher.EnqueueAsync(
            new NotificationEnqueueRequest(
                application.ParentUserId,
                eventType,
                culture,
                DeduplicationKeyBase: $"admission:{application.Id}:{actionKey}:{status}",
                Variables: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["applicationNumber"] = application.ApplicationNumber,
                    ["status"] = status,
                    ["schoolName"] = schoolName,
                },
                ActionPath: $"/parent/applications/{application.Id}",
                RelatedSchoolId: application.SchoolId,
                RelatedEntityId: application.Id),
            cancellationToken);
    }

    public static async Task EnqueueAsync(
        INotificationOutboxPublisher publisher,
        IParentAccountService parentAccountService,
        string schoolName,
        AdmissionApplication application,
        NotificationEventType eventType,
        string actionKey,
        CancellationToken cancellationToken)
    {
        var account = await parentAccountService.GetAsync(application.ParentUserId, cancellationToken);
        var culture = NormalizeCulture(account?.PreferredLanguage);
        var status = application.Status.ToString();

        await publisher.EnqueueAsync(
            new NotificationEnqueueRequest(
                application.ParentUserId,
                eventType,
                culture,
                DeduplicationKeyBase: $"admission:{application.Id}:{actionKey}:{status}",
                Variables: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["applicationNumber"] = application.ApplicationNumber,
                    ["status"] = status,
                    ["schoolName"] = schoolName,
                },
                ActionPath: $"/parent/applications/{application.Id}",
                RelatedSchoolId: application.SchoolId,
                RelatedEntityId: application.Id),
            cancellationToken);
    }

    private static string NormalizeCulture(string? preferredLanguage)
    {
        if (string.IsNullOrWhiteSpace(preferredLanguage))
        {
            return "ar";
        }

        var value = preferredLanguage.Trim().ToLowerInvariant();
        if (value.StartsWith("en", StringComparison.Ordinal))
        {
            return "en";
        }

        return "ar";
    }
}
