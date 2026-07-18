using System.Text.Json;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Notifications;

public sealed class NotificationOutboxPublisher(
    INotificationRepository repository,
    INotificationTemplateRenderer templateRenderer) : INotificationOutboxPublisher
{
    public async Task EnqueueAsync(
        NotificationEnqueueRequest request,
        CancellationToken cancellationToken = default)
    {
        var preferences = await repository.GetOrCreatePreferencesAsync(
            request.RecipientUserId,
            cancellationToken);

        var channels = ResolveChannels(request, preferences);
        if (channels.Count == 0)
        {
            return;
        }

        var culture = string.IsNullOrWhiteSpace(request.Culture)
            ? "ar"
            : request.Culture.Trim().ToLowerInvariant();

        foreach (var channel in channels)
        {
            var deduplicationKey = $"{request.DeduplicationKeyBase}:{channel}";
            if (await repository.OutboxExistsAsync(deduplicationKey, cancellationToken))
            {
                continue;
            }

            var rendered = templateRenderer.TryRender(
                request.EventType,
                channel,
                culture,
                request.Variables);

            if (rendered is null && channel != NotificationChannel.InApp)
            {
                continue;
            }

            string? subject;
            string body;
            Guid? templateVersionId;
            int templateVersionNumber;
            string templateCode;

            if (rendered is null)
            {
                subject = null;
                body = BuildFallbackBody(request.EventType, request.Variables);
                templateVersionId = null;
                templateVersionNumber = 0;
                templateCode = $"fallback.{request.EventType}";
            }
            else
            {
                subject = rendered.Value.Subject;
                body = rendered.Value.Body;
                templateVersionId = rendered.Value.VersionId;
                templateVersionNumber = rendered.Value.VersionNumber;
                templateCode = rendered.Value.TemplateCode;
            }

            var message = new NotificationOutboxMessage(
                request.RecipientUserId,
                request.EventType,
                channel,
                culture,
                templateVersionId,
                templateCode,
                templateVersionNumber,
                subject,
                body,
                deduplicationKey,
                BuildSafeMetadata(request),
                request.RelatedSchoolId,
                request.RelatedEntityId);

            if (!string.IsNullOrWhiteSpace(request.ActionPath))
            {
                message.SetActionPath(request.ActionPath);
            }

            await repository.AddOutboxAsync(message, cancellationToken);
        }
    }

    private static IReadOnlyList<NotificationChannel> ResolveChannels(
        NotificationEnqueueRequest request,
        ParentNotificationPreference preferences)
    {
        if (request.ForceChannels is { Count: > 0 })
        {
            return request.ForceChannels
                .Where(channel => IsChannelAllowed(request, preferences, channel))
                .Distinct()
                .ToArray();
        }

        var channels = new List<NotificationChannel>();
        var isMandatory = NotificationEventClassification.IsMandatory(request.EventType);

        if (isMandatory)
        {
            if (request.SkipPreferenceCheck || preferences.InAppEnabled)
            {
                channels.Add(NotificationChannel.InApp);
            }

            if (request.SkipPreferenceCheck || preferences.EmailEnabled)
            {
                channels.Add(NotificationChannel.Email);
            }

            return channels;
        }

        // Optional: AdmissionsOpened
        if (!request.SkipPreferenceCheck && !preferences.OptionalAdmissionsOpenEnabled)
        {
            return Array.Empty<NotificationChannel>();
        }

        if (request.SkipPreferenceCheck || preferences.InAppEnabled)
        {
            channels.Add(NotificationChannel.InApp);
        }

        if (request.SkipPreferenceCheck || preferences.EmailEnabled)
        {
            channels.Add(NotificationChannel.Email);
        }

        if (request.SkipPreferenceCheck || preferences.SmsEnabled)
        {
            channels.Add(NotificationChannel.Sms);
        }

        if (request.SkipPreferenceCheck || preferences.WhatsAppEnabled)
        {
            channels.Add(NotificationChannel.WhatsApp);
        }

        return channels;
    }

    private static bool IsChannelAllowed(
        NotificationEnqueueRequest request,
        ParentNotificationPreference preferences,
        NotificationChannel channel)
    {
        if (request.SkipPreferenceCheck)
        {
            return true;
        }

        if (!NotificationEventClassification.IsMandatory(request.EventType) &&
            !preferences.OptionalAdmissionsOpenEnabled)
        {
            return false;
        }

        return channel switch
        {
            NotificationChannel.InApp => preferences.InAppEnabled,
            NotificationChannel.Email => preferences.EmailEnabled,
            NotificationChannel.Sms => preferences.SmsEnabled,
            NotificationChannel.WhatsApp => preferences.WhatsAppEnabled,
            _ => false,
        };
    }

    private static string BuildFallbackBody(
        NotificationEventType eventType,
        IReadOnlyDictionary<string, string> variables)
    {
        if (variables.Count == 0)
        {
            return $"Schoolera notification: {eventType}";
        }

        var summary = string.Join(
            "; ",
            variables
                .Where(pair => !LooksSensitive(pair.Key))
                .Take(8)
                .Select(pair => $"{pair.Key}={pair.Value}"));

        return string.IsNullOrWhiteSpace(summary)
            ? $"Schoolera notification: {eventType}"
            : summary;
    }

    private static bool LooksSensitive(string key) =>
        key.Contains("code", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("otp", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("secret", StringComparison.OrdinalIgnoreCase);

    private static string? BuildSafeMetadata(NotificationEnqueueRequest request)
    {
        var payload = new Dictionary<string, string?>
        {
            ["eventType"] = request.EventType.ToString(),
        };

        if (request.RelatedSchoolId is Guid schoolId)
        {
            payload["relatedSchoolId"] = schoolId.ToString();
        }

        if (request.RelatedEntityId is Guid entityId)
        {
            payload["relatedEntityId"] = entityId.ToString();
        }

        return JsonSerializer.Serialize(payload);
    }
}
