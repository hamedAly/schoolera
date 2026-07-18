using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed class AdmissionInterviewAssessmentPolicySnapshotService(
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    IAdmissionApplicationRepository admissionRepository,
    ILogger<AdmissionInterviewAssessmentPolicySnapshotService> logger)
    : IAdmissionInterviewAssessmentPolicySnapshotService
{
    public async Task EnsureSnapshotAsync(
        AdmissionApplication application,
        Guid actorUserId,
        bool forceReplace = false,
        CancellationToken cancellationToken = default)
    {
        var hasSnapshot = application.PolicySnapshot is not null ||
            await HasSnapshotAsync(application.Id, cancellationToken);

        if (!forceReplace && hasSnapshot)
        {
            return;
        }

        if (forceReplace && await HasOperationalRecordsAsync(application.Id, cancellationToken))
        {
            logger.LogWarning(
                "Skipped policy snapshot replace for application {ApplicationId}: operational records exist.",
                application.Id);
            return;
        }

        if (forceReplace && hasSnapshot)
        {
            if (application.PolicySnapshot is not null)
            {
                await policyRepository.RemoveApplicationSnapshotAsync(
                    application.PolicySnapshot,
                    cancellationToken);
                application.ClearPolicySnapshot();
            }
            else
            {
                var existing = await policyRepository.GetApplicationSnapshotAsync(
                    application.Id,
                    cancellationToken);
                if (existing is not null)
                {
                    await policyRepository.RemoveApplicationSnapshotAsync(existing, cancellationToken);
                    application.ClearPolicySnapshot();
                }
            }
        }

        var published = await policyRepository.ListPublishedActiveAsync(
            application.SchoolId,
            cancellationToken);
        var applicable = InterviewAssessmentPolicyCatalog.ResolveApplicable(
            published,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId);

        if (applicable is null)
        {
            logger.LogInformation(
                "No applicable published interview/assessment policy for application {ApplicationId}.",
                application.Id);
            return;
        }

        var snapshot = AdmissionApplicationInterviewAssessmentPolicySnapshot.FromPolicy(
            application.Id,
            applicable,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId);

        application.AddPolicySnapshot(snapshot);

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: application.Status,
                toStatus: application.Status,
                action: forceReplace && hasSnapshot
                    ? AdmissionHistoryActions.PolicySnapshotReplaced
                    : AdmissionHistoryActions.PolicySnapshotCreated,
                actorUserId: actorUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: $"policy={applicable.Id:N};version={applicable.PolicyVersion}"));

        logger.LogInformation(
            "{Action} interview/assessment policy snapshot for application {ApplicationId} from policy {PolicyId} v{Version}.",
            forceReplace && hasSnapshot ? "Replaced" : "Created",
            application.Id,
            applicable.Id,
            applicable.PolicyVersion);
    }

    public Task<bool> HasSnapshotAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        policyRepository.HasApplicationSnapshotAsync(applicationId, cancellationToken);

    public Task<bool> HasOperationalRecordsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        admissionRepository.HasInterviewOrAssessmentAppointmentsAsync(applicationId, cancellationToken);
}
