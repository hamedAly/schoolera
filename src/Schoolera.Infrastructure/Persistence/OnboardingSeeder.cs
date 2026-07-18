using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence;

/// <summary>
/// Idempotently seeds generic, country-neutral onboarding document types. Codes are stable
/// and reused as the existence key so re-running the seed never creates duplicates.
/// </summary>
public sealed class OnboardingSeeder(
    SchooleraDbContext dbContext,
    ILogger<OnboardingSeeder> logger)
{
    private static readonly (string Code, string NameAr, string NameEn, bool IsRequired, int SortOrder)[] DocumentTypes =
    [
        ("organization-registration", "سجل تسجيل المنشأة", "Organization Registration", true, 1),
        ("educational-license", "الترخيص التعليمي", "Educational License", true, 2),
        ("tax-document", "المستند الضريبي", "Tax Document", false, 3),
        ("authorized-representative-letter", "خطاب تفويض الممثل", "Authorized Representative Letter", true, 4),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running onboarding document-type seed...");

        var existingCodes = await dbContext.SchoolOnboardingDocumentTypes
            .Select(type => type.Code)
            .ToListAsync(cancellationToken);

        var existing = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var inserted = 0;

        foreach (var (code, nameAr, nameEn, isRequired, sortOrder) in DocumentTypes)
        {
            if (existing.Contains(code))
            {
                continue;
            }

            await dbContext.SchoolOnboardingDocumentTypes.AddAsync(
                new SchoolOnboardingDocumentType(code, nameAr, nameEn, isRequired, sortOrder),
                cancellationToken);
            inserted++;
        }

        if (inserted > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Onboarding seed completed. Document types inserted={Inserted}.", inserted);
    }
}
