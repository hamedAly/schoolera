using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Database-backed notification outbox row. Never stores provider credentials.</summary>
public sealed class NotificationOutboxMessage
{
    private NotificationOutboxMessage()
    {
    }

    public NotificationOutboxMessage(
        Guid recipientUserId,
        NotificationEventType eventType,
        NotificationChannel channel,
        string culture,
        Guid? templateVersionId,
        string templateCode,
        int templateVersionNumber,
        string? subject,
        string bodyOrPayload,
        string deduplicationKey,
        string? safeMetadataJson,
        Guid? relatedSchoolId = null,
        Guid? relatedEntityId = null)
    {
        if (!Enum.IsDefined(eventType) || !Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException();
        }

        if (string.IsNullOrWhiteSpace(deduplicationKey))
        {
            throw new ArgumentException("Deduplication key is required.", nameof(deduplicationKey));
        }

        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        EventType = eventType;
        Channel = channel;
        Culture = culture.Trim().ToLowerInvariant();
        TemplateVersionId = templateVersionId;
        TemplateCode = templateCode.Trim();
        TemplateVersionNumber = templateVersionNumber;
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        BodyOrPayload = bodyOrPayload?.Trim() ?? string.Empty;
        Status = NotificationStatus.Pending;
        AttemptCount = 0;
        NextAttemptAtUtc = DateTimeOffset.UtcNow;
        DeduplicationKey = deduplicationKey.Trim();
        SafeMetadataJson = string.IsNullOrWhiteSpace(safeMetadataJson) ? null : safeMetadataJson.Trim();
        RelatedSchoolId = relatedSchoolId;
        RelatedEntityId = relatedEntityId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid RecipientUserId { get; private set; }

    public NotificationEventType EventType { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public string Culture { get; private set; } = "ar";

    public Guid? TemplateVersionId { get; private set; }

    public string TemplateCode { get; private set; } = string.Empty;

    public int TemplateVersionNumber { get; private set; }

    public string? Subject { get; private set; }

    public string BodyOrPayload { get; private set; } = string.Empty;

    public NotificationStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public DateTimeOffset? FailedAtUtc { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid? IntegrationConfigurationId { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public string DeduplicationKey { get; private set; } = string.Empty;

    public string? SafeMetadataJson { get; private set; }

    public string? LastSafeFailureCode { get; private set; }

    public Guid? RelatedSchoolId { get; private set; }

    public Guid? RelatedEntityId { get; private set; }

    /// <summary>InApp navigation path (server-controlled relative path only).</summary>
    public string? ActionPath { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void SetActionPath(string? actionPath)
    {
        if (string.IsNullOrWhiteSpace(actionPath))
        {
            ActionPath = null;
            return;
        }

        var path = actionPath.Trim();
        if (!path.StartsWith('/') || path.Contains("://", StringComparison.Ordinal) ||
            path.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("Action path must be a relative application path.", nameof(actionPath));
        }

        ActionPath = path;
        Touch();
    }

    public void MarkProcessing()
    {
        Status = NotificationStatus.Processing;
        ProcessingStartedAtUtc = DateTimeOffset.UtcNow;
        AttemptCount++;
        Touch();
    }

    public void MarkSent(Guid? integrationConfigurationId, string? providerMessageId)
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = DateTimeOffset.UtcNow;
        IntegrationConfigurationId = integrationConfigurationId;
        ProviderMessageId = Truncate(providerMessageId, 200);
        LastSafeFailureCode = null;
        Touch();
    }

    public void MarkDelivered()
    {
        Status = NotificationStatus.Delivered;
        DeliveredAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkFailed(string safeFailureCode, DateTimeOffset nextAttemptAtUtc)
    {
        Status = NotificationStatus.Failed;
        FailedAtUtc = DateTimeOffset.UtcNow;
        LastSafeFailureCode = Truncate(safeFailureCode, 100);
        NextAttemptAtUtc = nextAttemptAtUtc;
        Touch();
    }

    public void MarkDeadLetter(string safeFailureCode)
    {
        Status = NotificationStatus.DeadLetter;
        DeadLetteredAtUtc = DateTimeOffset.UtcNow;
        FailedAtUtc = DeadLetteredAtUtc;
        LastSafeFailureCode = Truncate(safeFailureCode, 100);
        Touch();
    }

    public void MarkSkipped(string safeFailureCode)
    {
        Status = NotificationStatus.Skipped;
        LastSafeFailureCode = Truncate(safeFailureCode, 100);
        Touch();
    }

    public void MarkCancelled(string? safeFailureCode = null)
    {
        Status = NotificationStatus.Cancelled;
        LastSafeFailureCode = Truncate(safeFailureCode, 100);
        Touch();
    }

    public void RequeuePending(DateTimeOffset nextAttemptAtUtc)
    {
        Status = NotificationStatus.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc;
        ProcessingStartedAtUtc = null;
        Touch();
    }

    public void MarkRead()
    {
        if (ReadAtUtc is not null)
        {
            return;
        }

        ReadAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
