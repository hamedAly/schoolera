using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed class AdmissionRequirementSnapshotService(
    ISchoolAdmissionRequirementRepository requirementRepository,
    IAdmissionApplicationRepository admissionRepository,
    ILogger<AdmissionRequirementSnapshotService> logger)
    : IAdmissionRequirementSnapshotService
{
    public async Task EnsureSnapshotsAsync(
        AdmissionApplication application,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if ((application.RequirementSnapshots?.Count ?? 0) > 0 ||
            await requirementRepository.HasApplicationSnapshotsAsync(application.Id, cancellationToken))
        {
            return;
        }

        var published = await requirementRepository.ListPublishedActiveAsync(
            application.SchoolId,
            cancellationToken);
        var applicable = AdmissionRequirementCatalog.ResolveApplicable(
            published,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId);

        // ForUpdate loads omit RequirementSnapshots; initialize only when empty in DB (checked above).
        application.EnsureRequirementSnapshotsCollection();

        foreach (var definition in applicable)
        {
            application.AddRequirementSnapshot(
                AdmissionApplicationRequirementSnapshot.FromDefinition(application.Id, definition));
        }

        // Never use application.AddHistory here: ForUpdate entities do not Include History, and
        // initializing an empty History collection would risk wiping existing timeline rows.
        if (applicable.Count > 0)
        {
            admissionRepository.AddHistory(
                new AdmissionApplicationHistory(
                    application.Id,
                    fromStatus: application.Status,
                    toStatus: application.Status,
                    action: AdmissionHistoryActions.RequirementsSnapshotCreated,
                    actorUserId: actorUserId,
                    actorRole: SchooleraRoles.Parent,
                    parentVisible: true,
                    parentVisibleNote: null,
                    internalNote: $"count={applicable.Count}"));
        }

        logger.LogInformation(
            "Created {Count} admission requirement snapshots for application {ApplicationId}.",
            applicable.Count,
            application.Id);
    }

    public Task<bool> HasSnapshotsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        requirementRepository.HasApplicationSnapshotsAsync(applicationId, cancellationToken);
}
