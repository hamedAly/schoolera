using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdmissionQuestion;

public sealed record UpdateSchoolAdmissionQuestionCommand(
    Guid SchoolId,
    Guid QuestionId,
    UpdateSchoolAdmissionQuestionRequest Body)
    : IRequest<Result<SchoolAdmissionQuestionDetailDto>>;

public sealed class UpdateSchoolAdmissionQuestionCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionQuestionRepository questionRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolAdmissionQuestionCommandHandler> logger)
    : IRequestHandler<UpdateSchoolAdmissionQuestionCommand, Result<SchoolAdmissionQuestionDetailDto>>
{
    public async Task<Result<SchoolAdmissionQuestionDetailDto>> Handle(
        UpdateSchoolAdmissionQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<SchoolAdmissionQuestionDetailDto>.Failure(access.Errors, access.ErrorCodes);
        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolAdmissionQuestionDetailDto>(
            access.Data!, SchoolPortalPermission.ManageAdmissionQuestions, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var entity = await questionRepository.GetByIdForUpdateAsync(
            request.SchoolId, request.QuestionId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionNotFound);
        }

        if (entity.PublicationStatus == AdmissionQuestionPublicationStatus.Published)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionMustUnpublish);
        }

        var body = request.Body;
        if (AdmissionScope.ComputeSpecificityScore(
                body.SchoolBranchId,
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId) <= 0)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionInvalidScope);
        }

        if (body.MaxFileSizeBytes is { } size && size > privateFileStorage.Policy.MaxFileSizeBytes)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionInvalidOptions);
        }

        try
        {
            entity.UpdateDraft(
                body.LabelAr,
                body.LabelEn,
                body.HelpAr,
                body.HelpEn,
                body.IsRequired,
                body.SortOrder,
                body.SchoolBranchId,
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId,
                body.MinLength,
                body.MaxLength,
                body.MinSelectedOptions,
                body.MaxSelectedOptions,
                body.MinDate,
                body.MaxDate,
                AdmissionRequirementCatalog.SerializeExtensions(body.AllowedFileExtensions),
                body.MaxFileSizeBytes,
                body.AllowChildVaultCopy,
                access.Data.UserId);

            if (AdmissionQuestionCatalog.IsChoiceType(entity.QuestionType))
            {
                entity.ReplaceOptions(
                    SchoolAdmissionQuestionMapping.ToDomainOptions(entity.Id, body.Options),
                    access.Data.UserId);
            }
        }
        catch (InvalidOperationException)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionMustUnpublish);
        }

        await questionRepository.AddAuditAsync(
            new Domain.Entities.SchoolAdmissionQuestionAudit(
                request.SchoolId,
                entity.Id,
                Domain.Entities.SchoolAdmissionQuestionAuditActions.Updated,
                access.Data.UserId,
                metadata: entity.QuestionCode),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolAdmissionQuestionDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await questionRepository.GetByIdAsync(request.SchoolId, entity.Id, cancellationToken)
            ?? entity;
        logger.LogInformation(
            "Updated admission question {QuestionId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolAdmissionQuestionDetailDto>.Success(
            SchoolAdmissionQuestionMapping.ToDetail(loaded));
    }
}
