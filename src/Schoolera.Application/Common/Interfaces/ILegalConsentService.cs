using Schoolera.Application.Common.Models;
using Schoolera.Application.Legal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// Resolves current mandatory legal document versions and persists append-only acceptances.
/// Clients send acceptance booleans only; the server resolves version IDs.
/// </summary>
public interface ILegalConsentService
{
    Task<CurrentLegalDocumentsDto> GetCurrentDocumentsAsync(CancellationToken cancellationToken = default);

    Task<Result<bool>> EnsureCurrentVersionsExistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists missing current Terms/Privacy acceptances for the purpose when the caller has accepted.
    /// Skips exact user/version/purpose duplicates; does not invent historical consent.
    /// </summary>
    Task<Result<bool>> PersistCurrentAcceptancesAsync(
        Guid userId,
        LegalAcceptancePurpose purpose,
        bool termsAccepted,
        bool privacyAccepted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates immutable legal versions from sanitized CMS Terms/Privacy content when content changed.
    /// </summary>
    Task PublishVersionsFromCmsPageAsync(
        string slug,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default);
}
