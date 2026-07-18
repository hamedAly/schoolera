using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolPortal.Options;
using Schoolera.Infrastructure.Storage;

namespace Schoolera.Infrastructure.Storage;

public sealed class SchoolPortalMediaOrphanCleanup(
    ISchoolPortalRepository repository,
    IOptions<FileStorageOptions> fileStorageOptions,
    ILogger<SchoolPortalMediaOrphanCleanup> logger) : ISchoolPortalMediaCleanup
{
    private static readonly string[] PortalCategories =
    [
        SchoolPortalMediaOptions.LogosCategory,
        SchoolPortalMediaOptions.CoversCategory,
        SchoolPortalMediaOptions.GalleryCategory,
    ];

    public async Task<SchoolPortalMediaCleanupResult> CleanupOrphansAsync(
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var settings = fileStorageOptions.Value;
        var referenced = await repository.GetReferencedMediaUrlsAsync(cancellationToken);
        var publicPrefix = settings.PublicRequestPath.TrimEnd('/');

        var scanned = 0;
        var orphans = 0;
        var deleted = 0;

        foreach (var category in PortalCategories)
        {
            var categoryRoot = Path.Combine(settings.StorageRoot, category.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(categoryRoot))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(categoryRoot, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                scanned++;

                var relative = Path.GetRelativePath(settings.StorageRoot, file).Replace('\\', '/');
                var publicUrl = $"{publicPrefix}/{relative}";

                if (referenced.Contains(publicUrl))
                {
                    continue;
                }

                orphans++;
                if (dryRun)
                {
                    logger.LogInformation("Dry-run orphan: {PublicUrl}", publicUrl);
                    continue;
                }

                File.Delete(file);
                deleted++;
                logger.LogInformation("Deleted orphan portal media: {PublicUrl}", publicUrl);
            }
        }

        return new SchoolPortalMediaCleanupResult(scanned, orphans, deleted, dryRun);
    }
}
