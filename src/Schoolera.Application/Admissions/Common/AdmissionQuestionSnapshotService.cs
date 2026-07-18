using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed class AdmissionQuestionSnapshotService(
    ISchoolAdmissionQuestionRepository questionRepository,
    IAdmissionApplicationRepository admissionRepository,
    ILogger<AdmissionQuestionSnapshotService> logger)
    : IAdmissionQuestionSnapshotService
{
    public async Task EnsureSnapshotsAsync(
        AdmissionApplication application,
        Guid actorUserId,
        bool forceReplace = false,
        CancellationToken cancellationToken = default)
    {
        var hasSnapshots = (application.QuestionSnapshots?.Count ?? 0) > 0 ||
            await questionRepository.HasApplicationSnapshotsAsync(application.Id, cancellationToken);

        if (hasSnapshots && !forceReplace)
        {
            return;
        }

        if (forceReplace && hasSnapshots)
        {
            application.ClearQuestionSnapshotsAndAnswers();
            admissionRepository.AddHistory(
                new AdmissionApplicationHistory(
                    application.Id,
                    fromStatus: application.Status,
                    toStatus: application.Status,
                    action: AdmissionHistoryActions.QuestionsSnapshotReset,
                    actorUserId: actorUserId,
                    actorRole: SchooleraRoles.Parent,
                    parentVisible: true,
                    parentVisibleNote: null,
                    internalNote: null));
        }

        var published = await questionRepository.ListPublishedActiveAsync(
            application.SchoolId,
            cancellationToken);
        var applicable = AdmissionQuestionCatalog.ResolveApplicable(
            published,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId);

        application.EnsureQuestionSnapshotsCollection();

        foreach (var definition in applicable)
        {
            application.AddQuestionSnapshot(
                AdmissionApplicationQuestionSnapshot.FromDefinition(application.Id, definition));
        }

        if (applicable.Count > 0)
        {
            admissionRepository.AddHistory(
                new AdmissionApplicationHistory(
                    application.Id,
                    fromStatus: application.Status,
                    toStatus: application.Status,
                    action: AdmissionHistoryActions.QuestionsSnapshotCreated,
                    actorUserId: actorUserId,
                    actorRole: SchooleraRoles.Parent,
                    parentVisible: true,
                    parentVisibleNote: null,
                    internalNote: $"count={applicable.Count}"));
        }

        logger.LogInformation(
            "Created {Count} admission question snapshots for application {ApplicationId}.",
            applicable.Count,
            application.Id);
    }

    public Task<bool> HasSnapshotsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        questionRepository.HasApplicationSnapshotsAsync(applicationId, cancellationToken);

    public Task<bool> HasAnswersAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        questionRepository.HasApplicationAnswersAsync(applicationId, cancellationToken);
}
