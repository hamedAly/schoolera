using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Public platform contact form submission (PlatformAdmin monitoring only).</summary>
public sealed class ContactRequest
{
    private ContactRequest()
    {
    }

    public ContactRequest(
        string reference,
        string name,
        string phone,
        string? email,
        string category,
        string subject,
        string message,
        string source)
    {
        Id = Guid.NewGuid();
        Reference = reference.Trim();
        Name = name.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Category = category.Trim().ToLowerInvariant();
        Subject = subject.Trim();
        Message = message.Trim();
        ConsentAccepted = true;
        Source = source.Trim().ToLowerInvariant();
        Status = ContactRequestStatus.New;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string Category { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public bool ConsentAccepted { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public ContactRequestStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public string? AdminNote { get; private set; }

    public void StartReview(Guid actorUserId, string? adminNote)
    {
        EnsureTransition(ContactRequestStatus.New, ContactRequestStatus.InReview);
        Status = ContactRequestStatus.InReview;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ReviewedByUserId = actorUserId;
        SetAdminNote(adminNote);
        Touch();
    }

    public void Resolve(Guid actorUserId, string? adminNote)
    {
        if (Status is not (ContactRequestStatus.New or ContactRequestStatus.InReview))
        {
            throw new InvalidOperationException("Invalid contact status transition.");
        }

        Status = ContactRequestStatus.Resolved;
        ReviewedAtUtc ??= DateTimeOffset.UtcNow;
        ReviewedByUserId = actorUserId;
        SetAdminNote(adminNote);
        Touch();
    }

    public void Close(Guid actorUserId, string? adminNote)
    {
        if (Status is ContactRequestStatus.Closed)
        {
            throw new InvalidOperationException("Invalid contact status transition.");
        }

        Status = ContactRequestStatus.Closed;
        ReviewedAtUtc ??= DateTimeOffset.UtcNow;
        ReviewedByUserId = actorUserId;
        SetAdminNote(adminNote);
        Touch();
    }

    private void SetAdminNote(string? adminNote)
    {
        if (!string.IsNullOrWhiteSpace(adminNote))
        {
            AdminNote = adminNote.Trim();
        }
    }

    private static void EnsureTransition(ContactRequestStatus from, ContactRequestStatus to)
    {
        // Intentionally narrow; callers also validate.
        _ = from;
        _ = to;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
