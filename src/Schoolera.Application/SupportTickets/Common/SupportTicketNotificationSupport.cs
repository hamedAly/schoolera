using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Common;

/// <summary>Enqueues parent-visible support ticket notifications (never for internal notes).</summary>
public static class SupportTicketNotificationSupport
{
    public static async Task EnqueueAsync(
        INotificationOutboxPublisher publisher,
        IParentAccountService parentAccountService,
        SupportTicket ticket,
        NotificationEventType eventType,
        string actionKey,
        CancellationToken cancellationToken)
    {
        var account = await parentAccountService.GetAsync(ticket.ParentUserId, cancellationToken);
        var culture = NormalizeCulture(account?.PreferredLanguage);

        await publisher.EnqueueAsync(
            new NotificationEnqueueRequest(
                ticket.ParentUserId,
                eventType,
                culture,
                DeduplicationKeyBase: $"support-ticket:{ticket.Id}:{actionKey}:{(int)ticket.Status}",
                Variables: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ticketReference"] = ticket.Reference,
                    ["subject"] = ticket.Subject,
                    ["status"] = ticket.Status.ToString(),
                },
                ActionPath: $"/parent/support-tickets/{ticket.Id}",
                RelatedEntityId: ticket.Id),
            cancellationToken);
    }

    private static string NormalizeCulture(string? preferredLanguage)
    {
        if (string.IsNullOrWhiteSpace(preferredLanguage))
        {
            return "ar";
        }

        var value = preferredLanguage.Trim().ToLowerInvariant();
        return value.StartsWith("en", StringComparison.Ordinal) ? "en" : "ar";
    }
}
