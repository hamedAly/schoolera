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
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolInterviewAssessmentPolicy;

public sealed record DeactivateSchoolInterviewAssessmentPolicyCommand(
    Guid SchoolId,
    Guid PolicyId) : IRequest<Result<SchoolInterviewAssessmentPolicyDetailDto>>;

public sealed class DeactivateSchoolInterviewAssessmentPolicyCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeactivateSchoolInterviewAssessmentPolicyCommandHandler> logger)
    : IRequestHandler<DeactivateSchoolInterviewAssessmentPolicyCommand, Result<SchoolInterviewAssessmentPolicyDetailDto>>
{
    public async Task<Result<SchoolInterviewAssessmentPolicyDetailDto>> Handle(
        DeactivateSchoolInterviewAssessmentPolicyCommand request,
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

        var entity = await policyRepository.GetByIdForUpdateAsync(
            request.SchoolId, request.PolicyId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewAssessmentPolicyDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewAssessmentPolicyNotFound);
        }

        entity.Deactivate(access.Data.UserId);

        await policyRepository.AddAuditAsync(
            new SchoolInterviewAssessmentPolicyAudit(
                request.SchoolId,
                entity.Id,
                SchoolInterviewAssessmentPolicyAuditActions.Deactivated,
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
            "Deactivated interview/assessment policy {PolicyId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolInterviewAssessmentPolicyDetailDto>.Success(
            SchoolInterviewAssessmentPolicyMapping.ToDetail(entity));
    }
}
