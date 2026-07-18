using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolInterviewAssessmentPolicy;

public sealed record CreateSchoolInterviewAssessmentPolicyCommand(
    Guid SchoolId,
    CreateSchoolInterviewAssessmentPolicyRequest Body)
    : IRequest<Result<SchoolInterviewAssessmentPolicyDetailDto>>;

public sealed class CreateSchoolInterviewAssessmentPolicyCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository portalRepository,
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolInterviewAssessmentPolicyCommandHandler> logger)
    : IRequestHandler<CreateSchoolInterviewAssessmentPolicyCommand, Result<SchoolInterviewAssessmentPolicyDetailDto>>
{
    public async Task<Result<SchoolInterviewAssessmentPolicyDetailDto>> Handle(
        CreateSchoolInterviewAssessmentPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<SchoolInterviewAssessmentPolicyDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolInterviewAssessmentPolicyDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var body = request.Body;
        var specificity = AdmissionScope.ComputeSpecificityScore(
            body.SchoolBranchId, body.EducationalStageId, body.GradeId, body.AcademicYearId);
        if (specificity <= 0)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewAssessmentPolicyDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewAssessmentPolicyInvalidScope);
        }

        if (body.SchoolBranchId is { } branchId)
        {
            var branch = await portalRepository.GetBranchForWriteAsync(
                request.SchoolId, branchId, cancellationToken);
            if (branch is null)
            {
                return SchoolPortalResults.FailureForCode<SchoolInterviewAssessmentPolicyDetailDto>(
                    localizer, SchoolPortalErrorCodes.BranchNotFound);
            }

            var branchCheck = SchoolPortalAccess.RequireBranch<SchoolInterviewAssessmentPolicyDetailDto>(
                access.Data!, branchId, localizer);
            if (!branchCheck.Succeeded)
            {
                return branchCheck;
            }
        }

        var entity = new SchoolInterviewAssessmentPolicy(
            request.SchoolId,
            body.RequirementMode,
            body.DeliveryMode,
            body.RequiredParticipants,
            body.ExpectedDurationMinutes,
            body.BookingWindowOpensDaysBefore,
            body.BookingWindowClosesDaysBefore,
            body.MinimumSchedulingLeadTimeHours,
            body.ParentReschedulingAllowed,
            body.MaxParentRescheduleAttempts,
            body.ParentCancellationAllowed,
            body.PreparationNotesAr,
            body.PreparationNotesEn,
            body.OnSiteInstructionsAr,
            body.OnSiteInstructionsEn,
            body.OnlineInstructionsAr,
            body.OnlineInstructionsEn,
            body.MeetingProviderCode,
            body.HybridSelectionAuthority,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId,
            access.Data.UserId);

        await policyRepository.AddAsync(entity, cancellationToken);
        await policyRepository.AddAuditAsync(
            new SchoolInterviewAssessmentPolicyAudit(
                request.SchoolId,
                entity.Id,
                SchoolInterviewAssessmentPolicyAuditActions.Created,
                access.Data.UserId,
                metadata: null),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolInterviewAssessmentPolicyDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Created interview/assessment policy {PolicyId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolInterviewAssessmentPolicyDetailDto>.Success(
            SchoolInterviewAssessmentPolicyMapping.ToDetail(entity));
    }
}
