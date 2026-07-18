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

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdmissionRequirements;

public sealed record ReorderSchoolAdmissionRequirementsCommand(
    Guid SchoolId,
    ReorderSchoolAdmissionRequirementsRequest Body)
    : IRequest<Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>>;

public sealed class ReorderSchoolAdmissionRequirementsCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionRequirementRepository requirementRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ReorderSchoolAdmissionRequirementsCommandHandler> logger)
    : IRequestHandler<ReorderSchoolAdmissionRequirementsCommand, Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>> Handle(
        ReorderSchoolAdmissionRequirementsCommand request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>.Failure(
                access.Errors, access.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>(
            access.Data!, SchoolPortalPermission.ManageAdmissionRequirements, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var active = (await requirementRepository.ListAsync(
            request.SchoolId,
            branchId: null,
            stageId: null,
            gradeId: null,
            academicYearId: null,
            kind: null,
            publicationStatus: null,
            isActive: true,
            cancellationToken)).ToArray();

        var orderedIds = request.Body.OrderedRequirementIds;
        if (orderedIds.Count != active.Length ||
            active.Select(item => item.Id).ToHashSet().SetEquals(orderedIds) == false)
        {
            return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>(
                localizer, SchoolPortalErrorCodes.InvalidReorder);
        }

        var updated = new List<SchoolAdmissionRequirement>(orderedIds.Count);
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var tracked = await requirementRepository.GetByIdForUpdateAsync(
                request.SchoolId,
                orderedIds[index],
                cancellationToken);
            if (tracked is null)
            {
                return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>(
                    localizer, SchoolPortalErrorCodes.AdmissionRequirementNotFound);
            }

            tracked.SetSortOrder(index, access.Data.UserId);
            updated.Add(tracked);
        }

        await requirementRepository.AddAuditAsync(
            new SchoolAdmissionRequirementAudit(
                request.SchoolId,
                requirementId: null,
                SchoolAdmissionRequirementAuditActions.Reordered,
                access.Data.UserId,
                requirementCode: null,
                metadata: $"count={orderedIds.Count}"),
            cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Reordered {Count} admission requirements for school {SchoolId}.",
            orderedIds.Count,
            request.SchoolId);

        return Result<IReadOnlyList<SchoolAdmissionRequirementListItemDto>>.Success(
            updated
                .OrderBy(item => item.SortOrder)
                .Select(SchoolAdmissionRequirementMapping.ToListItem)
                .ToArray());
    }
}
