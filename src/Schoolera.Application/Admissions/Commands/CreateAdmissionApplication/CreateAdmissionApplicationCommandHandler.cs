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
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.CreateAdmissionApplication;

public sealed record CreateAdmissionApplicationCommand(CreateAdmissionApplicationRequest Body)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class CreateAdmissionApplicationCommandHandler(
    ICurrentUser currentUser,
    IParentProfileRepository parentProfileRepository,
    IAdmissionEligibilityService eligibilityService,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionApplicationNumberGenerator numberGenerator,
    IAdmissionRequirementSnapshotService requirementSnapshotService,
    IAdmissionQuestionSnapshotService questionSnapshotService,
    IAdmissionInterviewAssessmentPolicySnapshotService policySnapshotService,
    IAdmissionChildAgeEligibilitySnapshotService ageEligibilitySnapshotService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<CreateAdmissionApplicationCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<CreateAdmissionApplicationCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        CreateAdmissionApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionApplicationDetailDto>(localizer);
        }

        var body = request.Body;

        var profile = await parentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = new ParentProfile(userId);
            await parentProfileRepository.AddAsync(profile, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            profile = await parentProfileRepository.GetByUserIdAsync(userId, cancellationToken) ?? profile;
        }

        var eligibility = await eligibilityService.ValidateAsync(
            new AdmissionEligibilityRequest(
                userId,
                body.ChildProfileId,
                body.SchoolId,
                body.SchoolSlug,
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
                excludeApplicationId: null,
                cancellationToken))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "An active admission application already exists for this selection.",
                AdmissionErrorCodes.DuplicateActiveApplication);
        }

        var applicationNumber = await numberGenerator.GenerateAsync(cancellationToken);
        var application = new AdmissionApplication(
            applicationNumber,
            userId,
            context.ParentProfile.Id,
            context.Child.Id,
            context.School.Id,
            context.Branch.Id,
            context.Stage.Id,
            context.Grade.Id,
            context.AcademicYear.Id,
            body.ParentNotes);

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: null,
                toStatus: AdmissionApplicationStatus.Draft,
                action: AdmissionHistoryActions.Created,
                actorUserId: userId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: null));

        await requirementSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken);
        await questionSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken: cancellationToken);
        await policySnapshotService.EnsureSnapshotAsync(application, userId, cancellationToken: cancellationToken);
        await ageEligibilitySnapshotService.EnsureSnapshotAsync(application, userId, cancellationToken: cancellationToken);

        await admissionRepository.AddAsync(application, cancellationToken);

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
            "Created admission application {ApplicationId} ({ApplicationNumber}) for parent {UserId}.",
            loaded.Id,
            loaded.ApplicationNumber,
            userId);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(loaded, identityProtector));
    }
}
