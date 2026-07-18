using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.UpdateAdmissionApplication;

public sealed record UpdateAdmissionApplicationCommand(
    Guid ApplicationId,
    UpdateAdmissionApplicationRequest Body)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class UpdateAdmissionApplicationCommandHandler(
    ICurrentUser currentUser,
    IAdmissionEligibilityService eligibilityService,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionRequirementSnapshotService requirementSnapshotService,
    IAdmissionQuestionSnapshotService questionSnapshotService,
    IAdmissionInterviewAssessmentPolicySnapshotService policySnapshotService,
    IAdmissionChildAgeEligibilitySnapshotService ageEligibilitySnapshotService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UpdateAdmissionApplicationCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<UpdateAdmissionApplicationCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        UpdateAdmissionApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionApplicationDetailDto>(localizer);
        }

        var application = await admissionRepository.GetOwnedForUpdateAsync(
            userId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.NotFound<AdmissionApplicationDetailDto>();
        }

        if (!AdmissionTransitionPolicy.CanParentEdit(application.Status))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Admission application is read-only.",
                AdmissionErrorCodes.ReadOnly);
        }

        var body = request.Body;
        if (body.RowVersion is { Length: > 0 } rowVersion &&
            application.RowVersion.Length > 0 &&
            !rowVersion.AsSpan().SequenceEqual(application.RowVersion))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ConcurrentUpdate);
        }

        var scopeChanged = application.SchoolBranchId != body.SchoolBranchId ||
            application.EducationalStageId != body.EducationalStageId ||
            application.GradeId != body.GradeId ||
            application.AcademicYearId != body.AcademicYearId;

        if (scopeChanged &&
            await questionSnapshotService.HasAnswersAsync(application.Id, cancellationToken) &&
            !body.ConfirmClearQuestionAnswers)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Changing the application selection would invalidate saved question answers.",
                AdmissionErrorCodes.QuestionAnswersBlockScopeChange);
        }

        var eligibility = await eligibilityService.ValidateAsync(
            new AdmissionEligibilityRequest(
                userId,
                application.ChildProfileId,
                application.SchoolId,
                SchoolSlug: null,
                body.SchoolBranchId,
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId),
            cancellationToken);
        if (!eligibility.Succeeded || eligibility.Data is null)
        {
            return Result<AdmissionApplicationDetailDto>.Failure(eligibility.Errors, eligibility.ErrorCodes);
        }

        var context = eligibility.Data;
        if (await admissionRepository.HasActiveDuplicateAsync(
                context.Child.Id,
                context.School.Id,
                context.Branch.Id,
                context.Grade.Id,
                context.AcademicYear.Id,
                application.Id,
                cancellationToken))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "An active admission application already exists for this selection.",
                AdmissionErrorCodes.DuplicateActiveApplication);
        }

        application.UpdateDraftSelection(
            context.Branch.Id,
            context.Stage.Id,
            context.Grade.Id,
            context.AcademicYear.Id,
            body.ParentNotes);

        if (!await requirementSnapshotService.HasSnapshotsAsync(application.Id, cancellationToken))
        {
            await requirementSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken);
        }

        if (scopeChanged)
        {
            if (await policySnapshotService.HasOperationalRecordsAsync(application.Id, cancellationToken))
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Changing the application selection is blocked because interview or assessment appointments already exist.",
                    AdmissionErrorCodes.PolicySnapshotScopeChangeBlocked);
            }

            await questionSnapshotService.EnsureSnapshotsAsync(
                application,
                userId,
                forceReplace: true,
                cancellationToken);
            await policySnapshotService.EnsureSnapshotAsync(
                application,
                userId,
                forceReplace: true,
                cancellationToken);
            await ageEligibilitySnapshotService.EnsureSnapshotAsync(
                application,
                userId,
                forceReplace: true,
                cancellationToken);
        }
        else if (!await questionSnapshotService.HasSnapshotsAsync(application.Id, cancellationToken))
        {
            await questionSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken: cancellationToken);
        }

        if (!scopeChanged && !await policySnapshotService.HasSnapshotAsync(application.Id, cancellationToken))
        {
            await policySnapshotService.EnsureSnapshotAsync(application, userId, cancellationToken: cancellationToken);
        }

        if (!scopeChanged && !await ageEligibilitySnapshotService.HasSnapshotAsync(application.Id, cancellationToken))
        {
            await ageEligibilitySnapshotService.EnsureSnapshotAsync(application, userId, cancellationToken: cancellationToken);
        }

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.Updated,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: null));

        var conflict = await AdmissionResults.TrySaveAsync<AdmissionApplicationDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        logger.LogInformation(
            "Updated admission application {ApplicationId} for parent {UserId}.",
            loaded.Id,
            userId);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(loaded, identityProtector));
    }
}
