using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Legal.Constants;
using Schoolera.Application.Legal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Legal;

public sealed class LegalConsentService(SchooleraDbContext dbContext) : ILegalConsentService
{
    private const string TermsPath = "/terms";
    private const string PrivacyPath = "/privacy";

    public async Task<CurrentLegalDocumentsDto> GetCurrentDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var culture = PreferredCulture();
        var terms = await GetCurrentVersionAsync(LegalDocumentType.Terms, culture, cancellationToken)
            ?? await GetCurrentVersionAsync(LegalDocumentType.Terms, "ar", cancellationToken)
            ?? throw new InvalidOperationException("Current terms version is missing.");
        var privacy = await GetCurrentVersionAsync(LegalDocumentType.Privacy, culture, cancellationToken)
            ?? await GetCurrentVersionAsync(LegalDocumentType.Privacy, "ar", cancellationToken)
            ?? throw new InvalidOperationException("Current privacy version is missing.");

        return new CurrentLegalDocumentsDto(
            new CurrentLegalDocumentDto(
                LegalDocumentType.Terms,
                TermsPath,
                terms.VersionNumber,
                terms.Title,
                terms.PublishedAtUtc),
            new CurrentLegalDocumentDto(
                LegalDocumentType.Privacy,
                PrivacyPath,
                privacy.VersionNumber,
                privacy.Title,
                privacy.PublishedAtUtc));
    }

    public async Task<Result<bool>> EnsureCurrentVersionsExistAsync(
        CancellationToken cancellationToken = default)
    {
        var hasTerms = await dbContext.LegalDocumentVersions.AsNoTracking()
            .AnyAsync(
                version => version.DocumentType == LegalDocumentType.Terms && version.IsCurrentMandatory,
                cancellationToken);
        var hasPrivacy = await dbContext.LegalDocumentVersions.AsNoTracking()
            .AnyAsync(
                version => version.DocumentType == LegalDocumentType.Privacy && version.IsCurrentMandatory,
                cancellationToken);

        if (hasTerms && hasPrivacy)
        {
            return Result<bool>.Success(true);
        }

        return Result<bool>.Failure(
            ["Current mandatory legal document versions are missing."],
            [LegalErrorCodes.CurrentVersionMissing]);
    }

    public async Task<Result<bool>> PersistCurrentAcceptancesAsync(
        Guid userId,
        LegalAcceptancePurpose purpose,
        bool termsAccepted,
        bool privacyAccepted,
        CancellationToken cancellationToken = default)
    {
        if (!termsAccepted)
        {
            return Result<bool>.Failure(
                ["Terms acceptance is required."],
                [LegalErrorCodes.TermsRequired]);
        }

        if (!privacyAccepted)
        {
            return Result<bool>.Failure(
                ["Privacy acceptance is required."],
                [LegalErrorCodes.PrivacyRequired]);
        }

        var ensure = await EnsureCurrentVersionsExistAsync(cancellationToken);
        if (!ensure.Succeeded)
        {
            return ensure;
        }

        // Persist acceptances for both cultures' current versions so language switches do not
        // re-require consent for the same published pair.
        var currentVersions = await dbContext.LegalDocumentVersions
            .Where(version => version.IsCurrentMandatory &&
                (version.DocumentType == LegalDocumentType.Terms ||
                 version.DocumentType == LegalDocumentType.Privacy))
            .ToListAsync(cancellationToken);

        if (currentVersions.Count == 0)
        {
            return Result<bool>.Failure(
                ["Current mandatory legal document versions are missing."],
                [LegalErrorCodes.CurrentVersionMissing]);
        }

        var versionIds = currentVersions.Select(version => version.Id).ToArray();
        var existing = await dbContext.LegalAcceptances
            .Where(acceptance =>
                acceptance.UserId == userId &&
                acceptance.Purpose == purpose &&
                versionIds.Contains(acceptance.LegalDocumentVersionId))
            .Select(acceptance => acceptance.LegalDocumentVersionId)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet();
        var now = DateTimeOffset.UtcNow;
        foreach (var version in currentVersions)
        {
            if (existingSet.Contains(version.Id))
            {
                continue;
            }

            dbContext.LegalAcceptances.Add(new LegalAcceptance(userId, version.Id, purpose, now));
        }

        return Result<bool>.Success(true);
    }

    public async Task PublishVersionsFromCmsPageAsync(
        string slug,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var documentType = slug.Trim().ToLowerInvariant() switch
        {
            "terms" => LegalDocumentType.Terms,
            "privacy" => LegalDocumentType.Privacy,
            _ => (LegalDocumentType?)null,
        };

        if (documentType is null)
        {
            return;
        }

        await PublishCultureAsync(
            documentType.Value,
            "ar",
            titleAr,
            contentAr,
            publishedAtUtc,
            cancellationToken);
        await PublishCultureAsync(
            documentType.Value,
            "en",
            titleEn,
            contentEn,
            publishedAtUtc,
            cancellationToken);
    }

    private async Task PublishCultureAsync(
        LegalDocumentType documentType,
        string culture,
        string title,
        string content,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken)
    {
        var current = await dbContext.LegalDocumentVersions
            .Where(version =>
                version.DocumentType == documentType &&
                version.Culture == culture &&
                version.IsCurrentMandatory)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is not null &&
            string.Equals(current.Title.Trim(), title.Trim(), StringComparison.Ordinal) &&
            string.Equals(current.Content.Trim(), content.Trim(), StringComparison.Ordinal))
        {
            return;
        }

        var maxVersion = await dbContext.LegalDocumentVersions
            .Where(version => version.DocumentType == documentType && version.Culture == culture)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var previousCurrents = await dbContext.LegalDocumentVersions
            .Where(version =>
                version.DocumentType == documentType &&
                version.Culture == culture &&
                version.IsCurrentMandatory)
            .ToListAsync(cancellationToken);

        foreach (var previous in previousCurrents)
        {
            previous.ClearCurrentMandatory();
        }

        dbContext.LegalDocumentVersions.Add(new LegalDocumentVersion(
            documentType,
            culture,
            maxVersion + 1,
            title,
            content,
            publishedAtUtc,
            isCurrentMandatory: true));
    }

    private Task<LegalDocumentVersion?> GetCurrentVersionAsync(
        LegalDocumentType documentType,
        string culture,
        CancellationToken cancellationToken) =>
        dbContext.LegalDocumentVersions.AsNoTracking()
            .Where(version =>
                version.DocumentType == documentType &&
                version.Culture == culture &&
                version.IsCurrentMandatory)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    private static string PreferredCulture()
    {
        var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return twoLetter.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
    }
}
