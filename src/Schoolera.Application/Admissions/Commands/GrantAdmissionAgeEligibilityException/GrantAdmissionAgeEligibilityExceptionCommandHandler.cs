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
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.GrantAdmissionAgeEligibilityException;

public sealed record GrantAdmissionAgeEligibilityExceptionRequest(
    ChildAgeEligibilityExceptionReasonCode ReasonCode,
    string? ReasonNote,
    byte[]? RowVersion);

public sealed record GrantAdmissionAgeEligibilityExceptionCommand(
    Guid SchoolId,
    Guid ApplicationId,
    GrantAdmissionAgeEligibilityExceptionRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class GrantAdmissionAgeEligibilityExceptionCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    ISchoolChildAgeEligibilityRuleRepository ruleRepository,
    IChildAgeEligibilityEvaluator evaluator,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<GrantAdmissionAgeEligibilityExceptionCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<GrantAdmissionAgeEligibilityExceptionCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        GrantAdmissionAgeEligibilityExceptionCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolAdmissionApplicationDetailDto>.Failure(
                accessResult.Errors,
                accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolAdmissionApplicationDetailDto>(
            access, SchoolPortalPermission.ManageApplicationReview, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var application = await admissionRepository.GetForSchoolForUpdateAsync(
            request.SchoolId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.ReviewNotFound<SchoolAdmissionApplicationDetailDto>();
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<SchoolAdmissionApplicationDetailDto>(
            access, application.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        if (AdmissionResults.HasRowVersionMismatch(request.Body.RowVersion, application.RowVersion))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ReviewConcurrentUpdate);
        }

        if (!AdmissionTransitionPolicy.CanSchoolGrantAgeException(application.Status))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Age exception is not allowed for the current application status.",
                AdmissionErrorCodes.AgeExceptionNotAllowed);
        }

        var birthDate = application.ChildProfile?.BirthDate;
        var evaluation = await evaluator.EvaluateAsync(
            application.SchoolId,
            application.SchoolBranchId,
            application.EducationalStageId,
            application.GradeId,
            application.AcademicYearId,
            birthDate,
            cancellationToken);

        if (evaluation.ResultCode is not (
            ChildAgeEligibilityResultCode.NotEligibleBelowMinimum or
            ChildAgeEligibilityResultCode.NotEligibleAboveMaximum))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Age exception is only allowed when the child is outside the configured age range.",
                AdmissionErrorCodes.AgeExceptionNotAllowed);
        }

        if (!evaluation.ManualExceptionAllowed)
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Manual age exceptions are not allowed for the applicable rule.",
                AdmissionErrorCodes.AgeExceptionNotAllowed);
        }

        var snapshot = application.AgeEligibilitySnapshot
            ?? await ruleRepository.GetApplicationSnapshotAsync(application.Id, cancellationToken);

        if (snapshot is null)
        {
            if (evaluation.RuleId is null || evaluation.ReferenceDate is null)
            {
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Age exception is not allowed without a configured rule.",
                    AdmissionErrorCodes.AgeExceptionNotAllowed);
            }

            var published = await ruleRepository.ListPublishedActiveAsync(
                application.SchoolId,
                cancellationToken);
            var rule = published.FirstOrDefault(item => item.Id == evaluation.RuleId.Value);
            if (rule is null)
            {
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "Age exception is not allowed without a configured rule.",
                    AdmissionErrorCodes.AgeExceptionNotAllowed);
            }

            snapshot = AdmissionApplicationChildAgeEligibilitySnapshot.FromRule(
                application.Id,
                rule,
                application.SchoolBranchId,
                application.EducationalStageId,
                application.GradeId,
                application.AcademicYearId,
                evaluation.ReferenceDate.Value,
                evaluation.CalculatedAgeCompletedMonths,
                evaluation.ResultCode,
                birthDate);
            application.AddAgeEligibilitySnapshot(snapshot);
        }

        if (snapshot.ManualExceptionIsApproved &&
            snapshot.ManualExceptionReasonCode == request.Body.ReasonCode)
        {
            var usersIdempotent = await userDirectory.GetUsersAsync(
                [application.ParentUserId],
                cancellationToken);
            usersIdempotent.TryGetValue(application.ParentUserId, out var parentUserIdempotent);
            return Result<SchoolAdmissionApplicationDetailDto>.Success(
                SchoolAdmissionMapping.ToSchoolDetail(
                    application,
                    SchoolAdmissionMapping.ToParentContact(parentUserIdempotent),
                    identityProtector,
                    ageEligibility: AgeEligibilityMapping.FromSnapshot(snapshot),
                    canGrantAgeException: false));
        }

        if (snapshot.ManualExceptionIsApproved)
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "An age exception has already been granted with a different reason.",
                AdmissionErrorCodes.AgeExceptionConflict);
        }

        try
        {
            snapshot.ApplyManualException(
                access.UserId,
                request.Body.ReasonCode,
                request.Body.ReasonNote);
        }
        catch (ArgumentOutOfRangeException)
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid age exception reason.",
                AdmissionErrorCodes.AgeExceptionNotAllowed);
        }

        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                application.Status,
                application.Status,
                AdmissionHistoryActions.AgeEligibilityExceptionGranted,
                access.UserId,
                SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: $"reason={request.Body.ReasonCode}"));

        var conflict = await AdmissionResults.TrySaveAsync<SchoolAdmissionApplicationDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return AdmissionResults.RemapSchoolSaveConflict(conflict);
        }

        logger.LogInformation(
            "Granted age eligibility exception for application {ApplicationId} by {UserId}.",
            application.Id,
            access.UserId);

        var loaded = await admissionRepository.GetForSchoolAsync(
            request.SchoolId,
            request.ApplicationId,
            cancellationToken) ?? application;
        var users = await userDirectory.GetUsersAsync([loaded.ParentUserId], cancellationToken);
        users.TryGetValue(loaded.ParentUserId, out var parentUser);

        return Result<SchoolAdmissionApplicationDetailDto>.Success(
            SchoolAdmissionMapping.ToSchoolDetail(
                loaded,
                SchoolAdmissionMapping.ToParentContact(parentUser),
                identityProtector,
                ageEligibility: AgeEligibilityMapping.FromSnapshot(loaded.AgeEligibilitySnapshot),
                canGrantAgeException: false));
    }
}
