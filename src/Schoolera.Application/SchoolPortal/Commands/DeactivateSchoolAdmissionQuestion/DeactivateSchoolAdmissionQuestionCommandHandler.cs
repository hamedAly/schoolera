using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolAdmissionQuestion;

public sealed record DeactivateSchoolAdmissionQuestionCommand(
    Guid SchoolId,
    Guid QuestionId) : IRequest<Result<SchoolAdmissionQuestionDetailDto>>;

public sealed class DeactivateSchoolAdmissionQuestionCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionQuestionRepository questionRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeactivateSchoolAdmissionQuestionCommandHandler> logger)
    : IRequestHandler<DeactivateSchoolAdmissionQuestionCommand, Result<SchoolAdmissionQuestionDetailDto>>
{
    public async Task<Result<SchoolAdmissionQuestionDetailDto>> Handle(
        DeactivateSchoolAdmissionQuestionCommand request,
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

        entity.Deactivate(access.Data.UserId);

        await questionRepository.AddAuditAsync(
            new SchoolAdmissionQuestionAudit(
                request.SchoolId,
                entity.Id,
                SchoolAdmissionQuestionAuditActions.Deactivated,
                access.Data.UserId,
                metadata: entity.QuestionCode),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolAdmissionQuestionDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Deactivated admission question {QuestionId} for school {SchoolId}.",
            entity.Id,
            request.SchoolId);
        return Result<SchoolAdmissionQuestionDetailDto>.Success(
            SchoolAdmissionQuestionMapping.ToDetail(entity));
    }
}
