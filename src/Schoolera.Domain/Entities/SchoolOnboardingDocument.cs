namespace Schoolera.Domain.Entities;

/// <summary>
/// Metadata for a privately stored onboarding document. The physical file lives outside
/// the public uploads root and is only reachable through an authorized download endpoint.
/// </summary>
public sealed class SchoolOnboardingDocument
{
    private SchoolOnboardingDocument()
    {
    }

    public SchoolOnboardingDocument(
        Guid applicationId,
        Guid documentTypeId,
        string originalFileName,
        string storedFileReference,
        string contentType,
        long fileSize,
        string? sha256Hash,
        Guid uploadedByUserId)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        DocumentTypeId = documentTypeId;
        OriginalFileName = originalFileName;
        StoredFileReference = storedFileReference;
        ContentType = contentType;
        FileSize = fileSize;
        Sha256Hash = sha256Hash;
        UploadedByUserId = uploadedByUserId;
        UploadedAtUtc = DateTimeOffset.UtcNow;
        IsCurrent = true;
    }

    public Guid Id { get; private set; }

    public Guid ApplicationId { get; private set; }

    public SchoolOnboardingApplication Application { get; private set; } = null!;

    public Guid DocumentTypeId { get; private set; }

    public SchoolOnboardingDocumentType DocumentType { get; private set; } = null!;

    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>Opaque storage reference (relative path); never a public URL or physical path.</summary>
    public string StoredFileReference { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long FileSize { get; private set; }

    public string? Sha256Hash { get; private set; }

    public DateTimeOffset UploadedAtUtc { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public DateTimeOffset? ReplacedAtUtc { get; private set; }

    public bool IsCurrent { get; private set; }

    /// <summary>Marks a previous file as retired so a new current file can be recorded.</summary>
    public void Retire(DateTimeOffset now)
    {
        IsCurrent = false;
        ReplacedAtUtc = now;
    }
}
