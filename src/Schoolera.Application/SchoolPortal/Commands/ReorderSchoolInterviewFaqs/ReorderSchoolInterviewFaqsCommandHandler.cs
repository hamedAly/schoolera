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

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolInterviewFaqs;

public sealed record ReorderSchoolInterviewFaqsCommand(
    Guid SchoolId,
    ReorderSchoolInterviewFaqsRequest Body) : IRequest<Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>>;

public sealed class ReorderSchoolInterviewFaqsCommandHandler(
    ISchoolPortalAccess portalAccess,
    ICmsRepository cmsRepository,
    ISchoolPortalAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ReorderSchoolInterviewFaqsCommandHandler> logger)
    : IRequestHandler<ReorderSchoolInterviewFaqsCommand, Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>> Handle(
        ReorderSchoolInterviewFaqsCommand request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>.Failure(access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<IReadOnlyList<SchoolInterviewFaqDetailDto>>(
            access.Data, SchoolPortalPermission.ManageContent, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var existing = await cmsRepository.ListSchoolInterviewFaqsAsync(
            request.SchoolId,
            interviewCategory: null,
            isPublished: null,
            isActive: null,
            branchId: null,
            stageId: null,
            gradeId: null,
            yearId: null,
            search: null,
            cancellationToken);

        var orderedIds = request.Body.OrderedIds;
        if (orderedIds.Count != existing.Count ||
            orderedIds.Distinct().Count() != orderedIds.Count ||
            !existing.Select(item => item.Id).ToHashSet().SetEquals(orderedIds))
        {
            return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolInterviewFaqDetailDto>>(
                localizer, SchoolPortalErrorCodes.InvalidReorder);
        }

        var resultItems = new List<SchoolInterviewFaqDetailDto>(orderedIds.Count);
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var item = await cmsRepository.GetSchoolFaqItemAsync(
                request.SchoolId, orderedIds[index], cancellationToken);
            if (item is null)
            {
                return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolInterviewFaqDetailDto>>(
                    localizer, SchoolPortalErrorCodes.InterviewFaqNotFound);
            }

            item.SetSortOrder(index);
            resultItems.Add(SchoolInterviewFaqSupport.ToDetail(item));
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<IReadOnlyList<SchoolInterviewFaqDetailDto>>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            access.Data.UserId,
            SchoolPortalAuditActions.InterviewFaqReordered,
            "FaqItem",
            request.SchoolId.ToString(),
            "Reordered school interview FAQs.",
            cancellationToken);

        logger.LogInformation("Reordered school interview FAQs for school {SchoolId}.", request.SchoolId);
        return Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>.Success(resultItems);
    }
}
