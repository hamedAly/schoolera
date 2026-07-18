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
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolInterviewAssessmentPolicy;

public sealed record PublishSchoolInterviewAssessmentPolicyCommand(
    Guid SchoolId,
    Guid PolicyId) : IRequest<Result<SchoolInterviewAssessmentPolicyDetailDto>>;

public sealed class PublishSchoolInterviewAssessmentPolicyCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository portalRepository,
    ISchoolInterviewAssessmentPolicyRepository policyRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<PublishSchoolInterviewAssessmentPolicyCommandHandler> logger)
    : IRequestHandler<PublishSchoolInterviewAssessmentPolicyCommand, Result<SchoolInterviewAssessmentPolicyDetailDto>>
{
    public async Task<Result<SchoolInterviewAssessmentPolicyDetailDto>> Handle(
        PublishSchoolInterviewAssessmentPolicyCommand request,
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

        SchoolBranch? branch = null;
        if (entity.SchoolBranchId is { } branchId)
        {
            branch = await portalRepository.GetBranchForWriteAsync(
                request.SchoolId, branchId, cancellationToken);
        }

        var meetingIntegrations = await notificationRepository.ListIntegrationsAsync(
            IntegrationType.Meeting,
            providerCode: null,
            isActive: true,
            healthStatus: null,
            cancellationToken);
        var meetingCodes = meetingIntegrations
            .Select(item => item.ProviderCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasActiveMeeting = meetingCodes.Count > 0;

        var validationCode = SchoolInterviewAssessmentPolicyMapping.ValidateForPublish(
            entity,
            branch,
            hasActiveMeeting,
            meetingCodes);
        if (validationCode is not null)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewAssessmentPolicyDetailDto>(
                localizer, validationCode);
        }

        if (await policyRepository.HasPublishedConflictAsync(
                request.SchoolId,
                entity.ScopeKey,
                entity.Id,
                cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewAssessmentPolicyDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewAssessmentPolicyConflict);
        }

        try
        {
            entity.Publish(access.Data.UserId);
        }
        catch (InvalidOperationException)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewAssessmentPolicyDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid);
        }

        await policyRepository.AddAuditAsync(
            new SchoolInterviewAssessmentPolicyAudit(
                request.SchoolId,
                entity.Id,
                SchoolInterviewAssessmentPolicyAuditActions.Published,
                access.Data.UserId,
                metadata: $"version={entity.PolicyVersion}"),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolInterviewAssessmentPolicyDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Published interview/assessment policy {PolicyId} v{Version} for school {SchoolId}.",
            entity.Id,
            entity.PolicyVersion,
            request.SchoolId);
        return Result<SchoolInterviewAssessmentPolicyDetailDto>.Success(
            SchoolInterviewAssessmentPolicyMapping.ToDetail(entity));
    }
}
