using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Append-only record that a user accepted a specific legal document version for a purpose.
/// No update/delete APIs — history is preserved when newer versions appear.
/// </summary>
public sealed class LegalAcceptance
{
    private LegalAcceptance()
    {
    }

    public LegalAcceptance(
        Guid userId,
        Guid legalDocumentVersionId,
        LegalAcceptancePurpose purpose,
        DateTimeOffset acceptedAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        LegalDocumentVersionId = legalDocumentVersionId;
        Purpose = purpose;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid LegalDocumentVersionId { get; private set; }

    public LegalDocumentVersion LegalDocumentVersion { get; private set; } = null!;

    public LegalAcceptancePurpose Purpose { get; private set; }

    public DateTimeOffset AcceptedAtUtc { get; private set; }
}
