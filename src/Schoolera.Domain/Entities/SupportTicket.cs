using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class SupportTicket
{
    private readonly List<SupportTicketMessage> _messages = [];
    private readonly List<SupportTicketHistory> _history = [];
    private readonly List<SupportTicketAttachment> _attachments = [];

    private SupportTicket()
    {
    }

    public SupportTicket(
        string reference,
        Guid parentUserId,
        SupportTicketCategory category,
        SupportTicketPriority priority,
        string subject,
        Guid? admissionApplicationId,
        Guid? sourceContactRequestId,
        DateTimeOffset firstResponseDueAtUtc,
        DateTimeOffset resolutionDueAtUtc)
    {
        if (!Enum.IsDefined(category) || !Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException();
        }

        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > 200)
        {
            throw new ArgumentException("Subject is required and must be at most 200 characters.", nameof(subject));
        }

        Id = Guid.NewGuid();
        Reference = reference.Trim();
        ParentUserId = parentUserId;
        AdmissionApplicationId = admissionApplicationId;
        SourceContactRequestId = sourceContactRequestId;
        Category = category;
        Priority = priority;
        Subject = subject.Trim();
        Status = SupportTicketStatus.Open;
        FirstResponseDueAtUtc = firstResponseDueAtUtc;
        ResolutionDueAtUtc = resolutionDueAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public Guid ParentUserId { get; private set; }

    public Guid? AdmissionApplicationId { get; private set; }

    public Guid? SourceContactRequestId { get; private set; }

    public SupportTicketCategory Category { get; private set; }

    public SupportTicketPriority Priority { get; private set; }

    public SupportTicketStatus Status { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public Guid? AssignedSupportAgentUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? FirstResponseAtUtc { get; private set; }

    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public DateTimeOffset FirstResponseDueAtUtc { get; private set; }

    public DateTimeOffset ResolutionDueAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<SupportTicketMessage> Messages => _messages;

    public IReadOnlyCollection<SupportTicketHistory> History => _history;

    public IReadOnlyCollection<SupportTicketAttachment> Attachments => _attachments;

    public void ChangeStatus(SupportTicketStatus next, DateTimeOffset utcNow)
    {
        Status = next;
        if (next is SupportTicketStatus.Resolved)
        {
            ResolvedAtUtc ??= utcNow;
        }

        if (next is SupportTicketStatus.Closed)
        {
            ClosedAtUtc ??= utcNow;
            ResolvedAtUtc ??= utcNow;
        }

        if (next is SupportTicketStatus.Open or SupportTicketStatus.InProgress)
        {
            if (next == SupportTicketStatus.Open && ResolvedAtUtc is not null)
            {
                ResolvedAtUtc = null;
                ClosedAtUtc = null;
            }
        }

        Touch();
    }

    public void SetPriority(SupportTicketPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        Priority = priority;
        Touch();
    }

    public void SetCategory(SupportTicketCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        Category = category;
        Touch();
    }

    public void Assign(Guid supportAgentUserId)
    {
        AssignedSupportAgentUserId = supportAgentUserId;
        Touch();
    }

    public void Unassign()
    {
        AssignedSupportAgentUserId = null;
        Touch();
    }

    public void RecordFirstResponse(DateTimeOffset utcNow)
    {
        FirstResponseAtUtc ??= utcNow;
        Touch();
    }

    public SupportTicketMessage AddMessage(
        Guid authorUserId,
        SupportTicketAuthorType authorType,
        SupportTicketMessageVisibility visibility,
        string body)
    {
        var message = new SupportTicketMessage(Id, authorUserId, authorType, visibility, body);
        _messages.Add(message);
        Touch();
        return message;
    }

    public void AddHistory(
        SupportTicketHistoryAction action,
        Guid? actorUserId,
        string? fromValue,
        string? toValue,
        string? summary)
    {
        _history.Add(new SupportTicketHistory(Id, action, actorUserId, fromValue, toValue, summary));
    }

    public SupportTicketAttachment AddAttachment(
        Guid? messageId,
        Guid uploadedByUserId,
        SupportTicketMessageVisibility visibility,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes)
    {
        var attachment = new SupportTicketAttachment(
            Id,
            messageId,
            uploadedByUserId,
            visibility,
            storageKey,
            originalFileName,
            contentType,
            sizeBytes);
        _attachments.Add(attachment);
        Touch();
        return attachment;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

public sealed class SupportTicketMessage
{
    private SupportTicketMessage()
    {
    }

    public SupportTicketMessage(
        Guid ticketId,
        Guid authorUserId,
        SupportTicketAuthorType authorType,
        SupportTicketMessageVisibility visibility,
        string body)
    {
        if (!Enum.IsDefined(authorType) || !Enum.IsDefined(visibility))
        {
            throw new ArgumentOutOfRangeException();
        }

        var trimmed = body?.Trim() ?? string.Empty;
        if (trimmed.Length is < 1 or > 4000)
        {
            throw new ArgumentException("Message body must be 1–4000 characters.", nameof(body));
        }

        Id = Guid.NewGuid();
        TicketId = ticketId;
        AuthorUserId = authorUserId;
        AuthorType = authorType;
        Visibility = visibility;
        Body = trimmed;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public SupportTicket Ticket { get; private set; } = null!;

    public Guid AuthorUserId { get; private set; }

    public SupportTicketAuthorType AuthorType { get; private set; }

    public SupportTicketMessageVisibility Visibility { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class SupportTicketHistory
{
    private SupportTicketHistory()
    {
    }

    public SupportTicketHistory(
        Guid ticketId,
        SupportTicketHistoryAction action,
        Guid? actorUserId,
        string? fromValue,
        string? toValue,
        string? summary)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        Action = action;
        ActorUserId = actorUserId;
        FromValue = Truncate(fromValue, 100);
        ToValue = Truncate(toValue, 100);
        Summary = Truncate(summary, 300);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public SupportTicket Ticket { get; private set; } = null!;

    public SupportTicketHistoryAction Action { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? FromValue { get; private set; }

    public string? ToValue { get; private set; }

    public string? Summary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim()[..Math.Min(value.Trim().Length, max)];
}

public sealed class SupportTicketAttachment
{
    private SupportTicketAttachment()
    {
    }

    public SupportTicketAttachment(
        Guid ticketId,
        Guid? messageId,
        Guid uploadedByUserId,
        SupportTicketMessageVisibility visibility,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        Id = Guid.NewGuid();
        TicketId = ticketId;
        MessageId = messageId;
        UploadedByUserId = uploadedByUserId;
        Visibility = visibility;
        StorageKey = storageKey.Trim();
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim().ToLowerInvariant();
        SizeBytes = sizeBytes;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public SupportTicket Ticket { get; private set; } = null!;

    public Guid? MessageId { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public SupportTicketMessageVisibility Visibility { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
