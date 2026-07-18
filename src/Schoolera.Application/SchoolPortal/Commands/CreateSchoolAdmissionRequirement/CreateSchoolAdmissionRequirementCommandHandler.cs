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

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdmissionRequirement;

public sealed record CreateSchoolAdmissionRequirementCommand(
    Guid SchoolId,
    CreateSchoolAdmissionRequirementRequest Body)
    : IRequest<Result<SchoolAdmissionRequirementDetailDto>>;

public sealed class CreateSchoolAdmissionRequirementCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolAdmissionRequirementCommandHandler> logger)
    : IRequestHandler<CreateSchoolAdmissionRequirementCommand, Result<SchoolAdmissionRequirementDetailDto>>
{
    public async Task<Result<SchoolAdmissionRequirementDetailDto>> Handle(
        CreateSchoolAdmissionRequirementCommand request,
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

        var body = request.Body;
        var specificity = SchoolAdmissionRequirement.ComputeSpecificityScore(
            body.SchoolBranchId, body.EducationalStageId, body.GradeId, body.AcademicYearId);
        if (specificity <= 0)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidScope);
        }

        if (body.Kind is AdmissionRequirementKind.ParentProfileField or AdmissionRequirementKind.ChildProfileField &&
            !AdmissionRequirementCatalog.IsValidProfileField(body.Kind, body.ProfileFieldCode))
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidField);
        }

        if (body.Kind == AdmissionRequirementKind.ApplicationDocument &&
            !AdmissionRequirementCatalog.IsValidDocumentCode(body.DocumentCode))
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidDocument);
        }

        var extensions = AdmissionRequirementCatalog.SerializeExtensions(body.AllowedFileExtensions);
        if (body.MaxFileSizeBytes is { } size && size > privateFileStorage.Policy.MaxFileSizeBytes)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidMaxSize);
        }

        SchoolAdmissionRequirement entity;
        try
        {
            entity = new SchoolAdmissionRequirement(
                request.SchoolId,
                body.RequirementCode,
                body.Kind,
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
                extensions,
                body.MaxFileSizeBytes,
                body.AllowChildVaultCopy,
                access.Data.UserId);
        }
        catch (ArgumentException)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionRequirementDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionRequirementInvalidKind);
        }

        await requirementRepository.AddAsync(entity, cancellationToken);
        await requirementRepository.AddAuditAsync(
            new SchoolAdmissionRequirementAudit(
                request.SchoolId,
                entity.Id,
                SchoolAdmissionRequirementAuditActions.Created,
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
            "Created admission requirement {RequirementId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolAdmissionRequirementDetailDto>.Success(
            SchoolAdmissionRequirementMapping.ToDetail(entity));
    }
}
