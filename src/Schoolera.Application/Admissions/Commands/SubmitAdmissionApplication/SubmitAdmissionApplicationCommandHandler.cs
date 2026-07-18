using System.Globalization;
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

namespace Schoolera.Application.Admissions.Commands.SubmitAdmissionApplication;

public sealed record SubmitAdmissionApplicationRequest(
    bool TermsAccepted,
    bool PrivacyAccepted);

public sealed record SubmitAdmissionApplicationCommand(
    Guid ApplicationId,
    SubmitAdmissionApplicationRequest Body)
    : IRequest<Result<SubmitAdmissionOutcomeDto>>;

public sealed class SubmitAdmissionApplicationCommandHandler(
    ICurrentUser currentUser,
    IAdmissionEligibilityService eligibilityService,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionRequirementSnapshotService requirementSnapshotService,
    IAdmissionRequirementCompletenessService requirementCompletenessService,
    IAdmissionQuestionSnapshotService questionSnapshotService,
    IAdmissionQuestionCompletenessService questionCompletenessService,
    IAdmissionChildAgeEligibilitySnapshotService ageEligibilitySnapshotService,
    IParentAccountService parentAccountService,
    ILegalConsentService legalConsentService,
    IUnitOfWork unitOfWork,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<SubmitAdmissionApplicationCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<SubmitAdmissionApplicationCommand, Result<SubmitAdmissionOutcomeDto>>
{
    public async Task<Result<SubmitAdmissionOutcomeDto>> Handle(
        SubmitAdmissionApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<SubmitAdmissionOutcomeDto>(localizer);
        }

        var application = await admissionRepository.GetOwnedForUpdateAsync(
            userId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.NotFound<SubmitAdmissionOutcomeDto>();
        }

        if (!AdmissionTransitionPolicy.TryValidateParentTransition(
                application.Status,
                AdmissionApplicationStatus.Submitted,
                application.ReviewStartedAtUtc,
                out var transitionCode))
        {
            return AdmissionResults.Failure<SubmitAdmissionOutcomeDto>(
                "Invalid status transition.",
                transitionCode);
        }

        var consent = await legalConsentService.PersistCurrentAcceptancesAsync(
            userId,
            LegalAcceptancePurpose.AdmissionSubmission,
            request.Body.TermsAccepted,
            request.Body.PrivacyAccepted,
            cancellationToken);
        if (!consent.Succeeded)
        {
            return Result<SubmitAdmissionOutcomeDto>.Failure(consent.Errors, consent.ErrorCodes);
        }

        var eligibility = await eligibilityService.ValidateAsync(
            new AdmissionEligibilityRequest(
                userId,
                application.ChildProfileId,
                application.SchoolId,
                SchoolSlug: null,
                application.SchoolBranchId,
                application.EducationalStageId,
                application.GradeId,
                application.AcademicYearId),
            cancellationToken);
        if (!eligibility.Succeeded || eligibility.Data is null)
        {
            return Result<SubmitAdmissionOutcomeDto>.Failure(eligibility.Errors, eligibility.ErrorCodes);
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
            return AdmissionResults.Failure<SubmitAdmissionOutcomeDto>(
                "An active admission application already exists for this selection.",
                AdmissionErrorCodes.DuplicateActiveApplication);
        }

        var account = await parentAccountService.GetAsync(userId, cancellationToken);
        if (account is null)
        {
            return AdmissionResults.Forbidden<SubmitAdmissionOutcomeDto>(localizer);
        }

        await requirementSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken);
        await questionSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken: cancellationToken);

        var ageEvaluation = await ageEligibilitySnapshotService.RecalculateForSubmitAsync(
            application,
            context.Child.BirthDate,
            userId,
            cancellationToken);

        if (!ageEvaluation.CanContinue)
        {
            var ageErrorCode = ageEvaluation.ResultCode switch
            {
                ChildAgeEligibilityResultCode.BirthDateRequired => AdmissionErrorCodes.AgeBirthDateRequired,
                ChildAgeEligibilityResultCode.InvalidBirthDate => AdmissionErrorCodes.AgeInvalidBirthDate,
                _ => AdmissionErrorCodes.AgeNotEligible,
            };

            return AdmissionResults.Failure<SubmitAdmissionOutcomeDto>(
                "The child does not meet the school's age eligibility requirements.",
                ageErrorCode);
        }

        var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        var culture = CultureInfo.CurrentUICulture.Name;

        var requirementEvaluation = await requirementCompletenessService.EvaluateAsync(
            loaded,
            culture,
            cancellationToken);

        if (!requirementEvaluation.IsComplete)
        {
            admissionRepository.AddHistory(
                new AdmissionApplicationHistory(
                    application.Id,
                    application.Status,
                    application.Status,
                    AdmissionHistoryActions.RequirementsValidationFailed,
                    userId,
                    SchooleraRoles.Parent,
                    parentVisible: true,
                    parentVisibleNote: null,
                    internalNote: $"count={requirementEvaluation.Missing.Count}"));

            var saveFailed = await AdmissionResults.TrySaveAsync<SubmitAdmissionOutcomeDto>(
                unitOfWork,
                cancellationToken);
            if (saveFailed is not null)
            {
                return saveFailed;
            }

            var missingRequirements = AdmissionMapping.ToMissingDtos(requirementEvaluation.Missing);
            var outcome = new SubmitAdmissionOutcomeDto(null, missingRequirements, []);
            return Result<SubmitAdmissionOutcomeDto>.Failure(
                outcome,
                ["Complete all required admission items before submitting."],
                [AdmissionErrorCodes.RequirementsIncomplete]);
        }

        var questionEvaluation = await questionCompletenessService.EvaluateAsync(
            loaded,
            culture,
            cancellationToken);

        if (!questionEvaluation.IsComplete)
        {
            admissionRepository.AddHistory(
                new AdmissionApplicationHistory(
                    application.Id,
                    application.Status,
                    application.Status,
                    AdmissionHistoryActions.QuestionsValidationFailed,
                    userId,
                    SchooleraRoles.Parent,
                    parentVisible: true,
                    parentVisibleNote: null,
                    internalNote: $"count={questionEvaluation.Missing.Count}"));

            var saveFailed = await AdmissionResults.TrySaveAsync<SubmitAdmissionOutcomeDto>(
                unitOfWork,
                cancellationToken);
            if (saveFailed is not null)
            {
                return saveFailed;
            }

            var missingQuestions = AdmissionMapping.ToMissingQuestionDtos(questionEvaluation.Missing);
            var outcome = new SubmitAdmissionOutcomeDto(
                null,
                Array.Empty<MissingAdmissionRequirementDto>(),
                missingQuestions);
            return Result<SubmitAdmissionOutcomeDto>.Failure(
                outcome,
                ["Complete all required admission questions before submitting."],
                [AdmissionErrorCodes.QuestionsIncomplete]);
        }

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.RequirementsCompleted,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: null));

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.QuestionsCompleted,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: null));

        var fromStatus = application.Status;
        var snapshot = AdmissionMapping.ToSnapshot(context, account, identityProtector);
        application.Submit(snapshot);
        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus,
                AdmissionApplicationStatus.Submitted,
                AdmissionHistoryActions.Submitted,
                userId,
                SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: null));

        var schoolName = context.School.NameAr
            ?? context.School.NameEn
            ?? application.SchoolId.ToString();
        await AdmissionParentNotificationSupport.EnqueueAsync(
            notificationOutboxPublisher,
            parentAccountService,
            schoolName,
            application,
            NotificationEventType.AdmissionApplicationSubmitted,
            "submitted",
            cancellationToken);

        var conflict = await AdmissionResults.TrySaveAsync<SubmitAdmissionOutcomeDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var submitted = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        logger.LogInformation(
            "Submitted admission application {ApplicationId} for parent {UserId}.",
            submitted.Id,
            userId);

        return Result<SubmitAdmissionOutcomeDto>.Success(
            new SubmitAdmissionOutcomeDto(
                AdmissionMapping.ToDetail(
                    submitted,
                    identityProtector,
                    requirementEvaluation,
                    questionEvaluation),
                Array.Empty<MissingAdmissionRequirementDto>(),
                Array.Empty<MissingAdmissionQuestionDto>()));
    }
}
