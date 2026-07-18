using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Common;

internal static class SchoolPortalBranchSlug
{
    public static async Task<string> CreateUniqueAsync(
        ISchoolPortalRepository repository,
        Guid schoolId,
        string nameAr,
        string? nameEn,
        Guid? excludeBranchId,
        CancellationToken cancellationToken)
    {
        var source = !string.IsNullOrWhiteSpace(nameEn) ? nameEn : nameAr;
        var baseSlug = SlugHelper.Normalize(source);
        var candidate = baseSlug;
        var suffix = 2;

        while (await repository.BranchSlugExistsAsync(schoolId, candidate, excludeBranchId, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }

        return candidate;
    }
}
