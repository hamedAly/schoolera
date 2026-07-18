using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListInterviewAssessmentSlots;

public sealed record ListInterviewAssessmentSlotsQuery(
    Guid SchoolId, Guid? BranchId, Guid? StageId, Guid? GradeId, Guid? AcademicYearId,
    SlotKind? Kind, SlotDeliveryMode? Mode, SlotStatus? Status, Guid? ResourceId,
    DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<Result<IReadOnlyList<InterviewAssessmentSlotDto>>>
{
    public static ListInterviewAssessmentSlotsQuery FromFilters(
        Guid schoolId, Guid? branchId, Guid? stageId, Guid? gradeId, Guid? academicYearId,
        int? kind, int? mode, int? status, Guid? resourceId, DateTimeOffset? from, DateTimeOffset? to) =>
        new(schoolId, branchId, stageId, gradeId, academicYearId,
            kind is { } kindValue ? (SlotKind?)kindValue : null,
            mode is { } modeValue ? (SlotDeliveryMode?)modeValue : null,
            status is { } statusValue ? (SlotStatus?)statusValue : null,
            resourceId, from, to);
}

public sealed class ListInterviewAssessmentSlotsQueryHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListInterviewAssessmentSlotsQuery, Result<IReadOnlyList<InterviewAssessmentSlotDto>>>
{
    public async Task<Result<IReadOnlyList<InterviewAssessmentSlotDto>>> Handle(
        ListInterviewAssessmentSlotsQuery request, CancellationToken cancellationToken)
    {
        var accessResult = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
            return Result<IReadOnlyList<InterviewAssessmentSlotDto>>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        var access = accessResult.Data;
        if (!access.HasPermission(SchoolPortalPermission.ManageAdmissionRequirements))
            return InterviewAssessmentSlotSupport.Fail<IReadOnlyList<InterviewAssessmentSlotDto>>(
                localizer, "AccessDenied", SchoolPortalErrorCodes.AccessDenied);
        if (request.BranchId is { } branch && !access.CanAccessBranch(branch))
            return InterviewAssessmentSlotSupport.Fail<IReadOnlyList<InterviewAssessmentSlotDto>>(
                localizer, "BranchScopeDenied", SchoolPortalErrorCodes.BranchOutOfScope);
        var rows = await repository.ListAsync(request.SchoolId, request.BranchId, request.StageId,
            request.GradeId, request.AcademicYearId, request.Kind, request.Mode, request.Status,
            request.ResourceId, request.From, request.To, cancellationToken);
        rows = access.AllowsAllBranches
            ? rows
            : rows.Where(x => access.CanAccessBranch(x.SchoolBranchId)).ToArray();
        var mapped = new List<InterviewAssessmentSlotDto>();
        foreach (var row in rows)
            mapped.Add(await InterviewAssessmentSlotSupport.MapAsync(row, repository, cancellationToken));
        return Result<IReadOnlyList<InterviewAssessmentSlotDto>>.Success(mapped);
    }
}
