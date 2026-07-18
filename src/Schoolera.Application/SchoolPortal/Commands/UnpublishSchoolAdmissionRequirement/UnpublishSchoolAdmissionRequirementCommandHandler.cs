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

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolAdmissionRequirement;

public sealed record UnpublishSchoolAdmissionRequirementCommand(
    Guid SchoolId,
    Guid RequirementId) : IRequest<Result<SchoolAdmissionRequirementDetailDto>>;

public sealed class UnpublishSchoolAdmissionRequirementCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UnpublishSchoolAdmissionRequirementCommandHandler> logger)
    : IRequestHandler<UnpublishSchoolAdmissionRequirementCommand, Result<SchoolAdmissionRequirementDetailDto>>
{
    public async Task<Result<SchoolAdmissionRequirementDetailDto>> Handle(
        UnpublishSchoolAdmissionRequirementCommand request,
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

        entity.Unpublish(access.Data.UserId);

        await requirementRepository.AddAuditAsync(
            new SchoolAdmissionRequirementAudit(
                request.SchoolId,
                entity.Id,
                SchoolAdmissionRequirementAuditActions.Unpublished,
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
            "Unpublished admission requirement {RequirementId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolAdmissionRequirementDetailDto>.Success(
            SchoolAdmissionRequirementMapping.ToDetail(entity));
    }
}
