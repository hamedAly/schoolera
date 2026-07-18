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

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdmissionQuestions;

public sealed record ReorderSchoolAdmissionQuestionsCommand(
    Guid SchoolId,
    ReorderSchoolAdmissionQuestionsRequest Body)
    : IRequest<Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>>;

public sealed class ReorderSchoolAdmissionQuestionsCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionQuestionRepository questionRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ReorderSchoolAdmissionQuestionsCommandHandler> logger)
    : IRequestHandler<ReorderSchoolAdmissionQuestionsCommand, Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>> Handle(
        ReorderSchoolAdmissionQuestionsCommand request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>.Failure(
                access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>(
            access.Data, SchoolPortalPermission.ManageAdmissionQuestions, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var active = (await questionRepository.ListAsync(
            request.SchoolId,
            branchId: null,
            stageId: null,
            gradeId: null,
            academicYearId: null,
            questionType: null,
            publicationStatus: null,
            isActive: true,
            cancellationToken)).ToArray();

        var orderedIds = request.Body.OrderedQuestionIds;
        if (orderedIds.Count != active.Length ||
            active.Select(item => item.Id).ToHashSet().SetEquals(orderedIds) == false)
        {
            return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>(
                localizer, SchoolPortalErrorCodes.InvalidReorder);
        }

        var updated = new List<Domain.Entities.SchoolAdmissionQuestion>(orderedIds.Count);
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var tracked = await questionRepository.GetByIdForUpdateAsync(
                request.SchoolId,
                orderedIds[index],
                cancellationToken);
            if (tracked is null)
            {
                return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>(
                    localizer, SchoolPortalErrorCodes.AdmissionQuestionNotFound);
            }

            tracked.SetSortOrder(index, access.Data.UserId);
            updated.Add(tracked);
        }

        await questionRepository.AddAuditAsync(
            new SchoolAdmissionQuestionAudit(
                request.SchoolId,
                questionId: null,
                SchoolAdmissionQuestionAuditActions.Reordered,
                access.Data.UserId,
                metadata: $"count={orderedIds.Count}"),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Reordered {Count} admission questions for school {SchoolId}.",
            orderedIds.Count,
            request.SchoolId);

        return Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>.Success(
            updated
                .OrderBy(item => item.SortOrder)
                .Select(SchoolAdmissionQuestionMapping.ToListItem)
                .ToArray());
    }
}
