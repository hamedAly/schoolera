namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolPortalMediaCleanup
{
    Task<SchoolPortalMediaCleanupResult> CleanupOrphansAsync(
        bool dryRun,
        CancellationToken cancellationToken = default);
}

public sealed record SchoolPortalMediaCleanupResult(
    int ScannedFiles,
    int OrphanFiles,
    int DeletedFiles,
    bool DryRun);
