using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Private parent attachment for an admission application. Storage keys are never exposed to clients.</summary>
public sealed class AdmissionApplicationAttachment
{
    private AdmissionApplicationAttachment()
    {
    }

    public AdmissionApplicationAttachment(
        Guid admissionApplicationId,
        AdmissionAttachmentType attachmentType,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string storageKey,
        Guid uploadedByUserId,
        Guid? sourceVaultDocumentId = null,
        Guid? requirementSnapshotId = null,
        AdmissionRequiredDocumentCode? requiredDocumentCode = null,
        Guid? questionSnapshotId = null)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        AttachmentType = attachmentType;
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        StorageKey = storageKey;
        UploadedByUserId = uploadedByUserId;
        SourceVaultDocumentId = sourceVaultDocumentId;
        RequirementSnapshotId = requirementSnapshotId;
        RequiredDocumentCode = requiredDocumentCode;
        QuestionSnapshotId = questionSnapshotId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public AdmissionApplication AdmissionApplication { get; private set; } = null!;

    public AdmissionAttachmentType AttachmentType { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long FileSizeBytes { get; private set; }

    /// <summary>Opaque private storage reference. Never return to Angular.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public Guid UploadedByUserId { get; private set; }

    /// <summary>
    /// Internal draft idempotency link to a Child Vault document.
    /// Never returned in DTOs. Application owns a separate storage key after copy.
    /// </summary>
    public Guid? SourceVaultDocumentId { get; private set; }

    /// <summary>Links a typed required-document upload to an immutable requirement snapshot.</summary>
    public Guid? RequirementSnapshotId { get; private set; }

    public AdmissionApplicationRequirementSnapshot? RequirementSnapshot { get; private set; }

    public AdmissionRequiredDocumentCode? RequiredDocumentCode { get; private set; }

    /// <summary>Links a file-question upload to an immutable question snapshot.</summary>
    public Guid? QuestionSnapshotId { get; private set; }

    public AdmissionApplicationQuestionSnapshot? QuestionSnapshot { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void ReplaceFile(
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string storageKey,
        Guid uploadedByUserId)
    {
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        StorageKey = storageKey;
        UploadedByUserId = uploadedByUserId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void LinkRequirementSnapshot(
        Guid requirementSnapshotId,
        AdmissionRequiredDocumentCode documentCode)
    {
        RequirementSnapshotId = requirementSnapshotId;
        RequiredDocumentCode = documentCode;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void LinkQuestionSnapshot(Guid questionSnapshotId)
    {
        QuestionSnapshotId = questionSnapshotId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
