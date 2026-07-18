using Schoolera.Domain.Enums;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Common.Interfaces;

public sealed record ResolvedIntegrationConfiguration(
    Guid Id,
    IntegrationType IntegrationType,
    string ProviderCode,
    string SettingsJson,
    int SettingsSchemaVersion,
    bool IsConfigured);

public interface IPlatformIntegrationConfigurationAccessor
{
    Task<ResolvedIntegrationConfiguration?> GetActiveDefaultAsync(
        IntegrationType integrationType,
        CancellationToken cancellationToken = default);

    void InvalidateCache(IntegrationType? integrationType = null);
}

public interface INotificationOutboxPublisher
{
    /// <summary>
    /// Enqueues channel notifications for an event. Deduplicates by stable key.
    /// Must be called inside the same unit-of-work/transaction as the business operation when possible.
    /// </summary>
    Task EnqueueAsync(NotificationEnqueueRequest request, CancellationToken cancellationToken = default);
}

public sealed record NotificationEnqueueRequest(
    Guid RecipientUserId,
    NotificationEventType EventType,
    string Culture,
    string DeduplicationKeyBase,
    IReadOnlyDictionary<string, string> Variables,
    string? ActionPath = null,
    Guid? RelatedSchoolId = null,
    Guid? RelatedEntityId = null,
    IReadOnlyList<NotificationChannel>? ForceChannels = null,
    bool SkipPreferenceCheck = false);

public interface INotificationTemplateRenderer
{
    (string? Subject, string Body, Guid VersionId, int VersionNumber, string TemplateCode)? TryRender(
        NotificationEventType eventType,
        NotificationChannel channel,
        string culture,
        IReadOnlyDictionary<string, string> variables);
}

public sealed record NotificationSendResult(
    bool Succeeded,
    bool IsRetryable,
    bool Skipped,
    string? ProviderMessageId,
    string? SafeFailureCode);

public interface INotificationChannelProvider
{
    NotificationChannel Channel { get; }

    Task<NotificationSendResult> SendAsync(
        NotificationOutboxMessage message,
        ResolvedIntegrationConfiguration? integration,
        CancellationToken cancellationToken = default);
}

public interface INotificationRepository
{
    Task<PlatformIntegrationConfiguration?> GetIntegrationAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformIntegrationConfiguration>> ListIntegrationsAsync(
        IntegrationType? type,
        string? providerCode,
        bool? isActive,
        IntegrationHealthStatus? healthStatus,
        CancellationToken cancellationToken = default);

    Task AddIntegrationAsync(PlatformIntegrationConfiguration entity, CancellationToken cancellationToken = default);

    Task ClearDefaultAsync(IntegrationType type, Guid? exceptId, CancellationToken cancellationToken = default);

    Task<ParentNotificationPreference> GetOrCreatePreferencesAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task<ParentNotificationPreference?> GetPreferencesAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task AddOutboxAsync(NotificationOutboxMessage message, CancellationToken cancellationToken = default);

    Task<bool> OutboxExistsAsync(string deduplicationKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationOutboxMessage>> ClaimPendingBatchAsync(
        int batchSize,
        DateTimeOffset staleBeforeUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationOutboxMessage>> ListParentInAppAsync(
        Guid parentUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountParentInAppAsync(Guid parentUserId, CancellationToken cancellationToken = default);

    Task<int> CountParentUnreadAsync(Guid parentUserId, CancellationToken cancellationToken = default);

    Task<NotificationOutboxMessage?> GetParentInAppAsync(
        Guid parentUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task MarkAllParentInAppReadAsync(Guid parentUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParentAdmissionOpenSubscription>> ListSubscriptionsAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default);

    Task<ParentAdmissionOpenSubscription?> GetSubscriptionAsync(
        Guid parentUserId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<ParentAdmissionOpenSubscription?> FindActiveSubscriptionAsync(
        Guid parentUserId,
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        CancellationToken cancellationToken = default);

    Task AddSubscriptionAsync(
        ParentAdmissionOpenSubscription subscription,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParentAdmissionOpenSubscription>> FindMatchingActiveSubscriptionsAsync(
        Guid schoolId,
        Guid? branchId,
        Guid stageId,
        IReadOnlyList<Guid> gradeIds,
        CancellationToken cancellationToken = default);

    Task<NotificationTemplate?> GetTemplateAsync(
        NotificationEventType eventType,
        NotificationChannel channel,
        string culture,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default);

    Task AddTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    Task<NotificationTemplateVersion?> GetTemplateVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<(int Pending, int Failed, int DeadLetter, int Processing)> GetOutboxCountsAsync(
        CancellationToken cancellationToken = default);
}
