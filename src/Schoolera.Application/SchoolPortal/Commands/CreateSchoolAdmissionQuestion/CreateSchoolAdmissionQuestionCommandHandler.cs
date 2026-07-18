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
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdmissionQuestion;

public sealed record CreateSchoolAdmissionQuestionCommand(
    Guid SchoolId,
    CreateSchoolAdmissionQuestionRequest Body)
    : IRequest<Result<SchoolAdmissionQuestionDetailDto>>;

public sealed class CreateSchoolAdmissionQuestionCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionQuestionRepository questionRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolAdmissionQuestionCommandHandler> logger)
    : IRequestHandler<CreateSchoolAdmissionQuestionCommand, Result<SchoolAdmissionQuestionDetailDto>>
{
    public async Task<Result<SchoolAdmissionQuestionDetailDto>> Handle(
        CreateSchoolAdmissionQuestionCommand request,
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

        if (AdmissionQuestionCatalog.IsChoiceType(body.QuestionType) &&
            (body.Options is null || body.Options.Count == 0))
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionInvalidOptions);
        }

        if (body.MaxFileSizeBytes is { } size && size > privateFileStorage.Policy.MaxFileSizeBytes)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionInvalidOptions);
        }

        SchoolAdmissionQuestion entity;
        try
        {
            entity = new SchoolAdmissionQuestion(
                request.SchoolId,
                body.QuestionCode,
                body.QuestionType,
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
        }
        catch (ArgumentException)
        {
            return SchoolPortalResults.FailureForCode<SchoolAdmissionQuestionDetailDto>(
                localizer, SchoolPortalErrorCodes.AdmissionQuestionInvalidType);
        }

        if (AdmissionQuestionCatalog.IsChoiceType(entity.QuestionType))
        {
            foreach (var option in SchoolAdmissionQuestionMapping.ToDomainOptions(entity.Id, body.Options))
            {
                entity.Options.Add(option);
            }
        }

        await questionRepository.AddAsync(entity, cancellationToken);
        await questionRepository.AddAuditAsync(
            new SchoolAdmissionQuestionAudit(
                request.SchoolId,
                entity.Id,
                SchoolAdmissionQuestionAuditActions.Created,
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
            "Created admission question {QuestionId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolAdmissionQuestionDetailDto>.Success(
            SchoolAdmissionQuestionMapping.ToDetail(loaded));
    }
}
