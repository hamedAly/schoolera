using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Private Parent-owned document in the Child Document Vault.
/// Independent from Admission Application attachments. Storage keys are never exposed to clients.
/// </summary>
public sealed class ChildDocument
{
    private ChildDocument()
    {
    }

    public ChildDocument(
        Guid childProfileId,
        Guid parentUserId,
        ChildDocumentType documentType,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string storageKey,
        Guid uploadedByUserId)
    {
        Id = Guid.NewGuid();
        ChildProfileId = childProfileId;
        ParentUserId = parentUserId;
        DocumentType = documentType;
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        StorageKey = storageKey;
        UploadedByUserId = uploadedByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ChildProfileId { get; private set; }

    public ChildProfile ChildProfile { get; private set; } = null!;

    /// <summary>Denormalized owner for ownership queries.</summary>
    public Guid ParentUserId { get; private set; }

    public ChildDocumentType DocumentType { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long FileSizeBytes { get; private set; }

    /// <summary>Opaque private storage reference. Never return to Angular.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public Guid UploadedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Replaces file metadata after a successful new private-file save.</summary>
    public void ReplaceFile(
        ChildDocumentType documentType,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string storageKey,
        Guid uploadedByUserId)
    {
        DocumentType = documentType;
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        StorageKey = storageKey;
        UploadedByUserId = uploadedByUserId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
