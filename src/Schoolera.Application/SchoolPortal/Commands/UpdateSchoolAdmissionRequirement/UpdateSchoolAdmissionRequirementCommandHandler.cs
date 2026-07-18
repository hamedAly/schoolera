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

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdmissionRequirement;

public sealed record UpdateSchoolAdmissionRequirementCommand(
    Guid SchoolId,
    Guid RequirementId,
    UpdateSchoolAdmissionRequirementRequest Body)
    : IRequest<Result<SchoolAdmissionRequirementDetailDto>>;

public sealed class UpdateSchoolAdmissionRequirementCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolAdmissionRequirementCommandHandler> logger)
    : IRequestHandler<UpdateSchoolAdmissionRequirementCommand, Result<SchoolAdmissionRequirementDetailDto>>
{
    public async Task<Result<SchoolAdmissionRequirementDetailDto>> Handle(
        UpdateSchoolAdmissionRequirementCommand request,
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
            request.SchoolId, request.RequirementId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementNotFound);
        }

        if (entity.PublicationStatus == AdmissionRequirementPublicationStatus.Published)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementMustUnpublish);
        }

        var body = request.Body;
        var specificity = SchoolAdmissionRequirement.ComputeSpecificityScore(
            body.SchoolBranchId, body.EducationalStageId, body.GradeId, body.AcademicYearId);
        if (specificity <= 0)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidScope);
        }

        if (entity.Kind is AdmissionRequirementKind.ParentProfileField or AdmissionRequirementKind.ChildProfileField &&
            !AdmissionRequirementCatalog.IsValidProfileField(entity.Kind, body.ProfileFieldCode))
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidField);
        }

        if (entity.Kind == AdmissionRequirementKind.ApplicationDocument &&
            !AdmissionRequirementCatalog.IsValidDocumentCode(body.DocumentCode))
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidDocument);
        }

        if (body.MaxFileSizeBytes is { } size && size > privateFileStorage.Policy.MaxFileSizeBytes)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidMaxSize);
        }

        try
        {
            entity.UpdateDraft(
                body.NameAr,
                body.NameEn,
                body.DescriptionAr,
                body.DescriptionEn,
                body.IsRequired,
                body.SortOrder,
                body.SchoolBranchId,
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId,
                body.ProfileFieldCode,
                body.DocumentCode,
                AdmissionRequirementCatalog.SerializeExtensions(body.AllowedFileExtensions),
                body.MaxFileSizeBytes,
                body.AllowChildVaultCopy,
                access.Data.UserId);
        }
        catch (InvalidOperationException)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementMustUnpublish);
        }

        await requirementRepository.AddAuditAsync(
            new SchoolAdmissionRequirementAudit(
                request.SchoolId,
                entity.Id,
                SchoolAdmissionRequirementAuditActions.Updated,
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
            "Updated admission requirement {RequirementId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolAdmissionRequirementDetailDto>.Success(
            SchoolAdmissionRequirementMapping.ToDetail(entity));
    }
}
