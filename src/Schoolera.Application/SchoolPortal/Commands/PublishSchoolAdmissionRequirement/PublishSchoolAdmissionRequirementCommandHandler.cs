using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolAdmissionRequirement;

public sealed record PublishSchoolAdmissionRequirementCommand(
    Guid SchoolId,
    Guid RequirementId) : IRequest<Result<SchoolAdmissionRequirementDetailDto>>;

public sealed class PublishSchoolAdmissionRequirementCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<PublishSchoolAdmissionRequirementCommandHandler> logger)
    : IRequestHandler<PublishSchoolAdmissionRequirementCommand, Result<SchoolAdmissionRequirementDetailDto>>
{
    public async Task<Result<SchoolAdmissionRequirementDetailDto>> Handle(
        PublishSchoolAdmissionRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<SchoolAdmissionRequirementDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolAdmissionRequirementDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var entity = await requirementRepository.GetByIdForUpdateAsync(
            request.SchoolId,
            request.RequirementId,
            cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementNotFound);
        }

        var validationCode = SchoolAdmissionRequirementMapping.ValidateForPublish(
            entity,
            privateFileStorage.Policy.MaxFileSizeBytes);
        if (validationCode is not null)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, validationCode);
        }

        if (await requirementRepository.HasPublishedConflictAsync(
                request.SchoolId,
                entity.RequirementCode,
                entity.ScopeKey,
                entity.Id,
                cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementConflict);
        }

        entity.Publish(access.Data.UserId);

        await requirementRepository.AddAuditAsync(
            new SchoolAdmissionRequirementAudit(
                request.SchoolId,
                entity.Id,
                SchoolAdmissionRequirementAuditActions.Published,
                access.Data.UserId,
                entity.RequirementCode,
                metadata: null),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolAdmissionRequirementDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Published admission requirement {RequirementId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolAdmissionRequirementDetailDto>.Success(
            SchoolAdmissionRequirementMapping.ToDetail(entity));
    }
}
