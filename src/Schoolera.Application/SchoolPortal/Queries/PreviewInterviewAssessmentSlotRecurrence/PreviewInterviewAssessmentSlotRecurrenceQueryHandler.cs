using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.PreviewInterviewAssessmentSlotRecurrence;

public sealed record PreviewInterviewAssessmentSlotRecurrenceQuery(
    Guid SchoolId, SlotRecurrenceRequest Body) : IRequest<Result<SlotRecurrencePreviewDto>>;

public sealed class PreviewInterviewAssessmentSlotRecurrenceQueryHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<PreviewInterviewAssessmentSlotRecurrenceQuery, Result<SlotRecurrencePreviewDto>>
{
    public async Task<Result<SlotRecurrencePreviewDto>> Handle(
        PreviewInterviewAssessmentSlotRecurrenceQuery request, CancellationToken cancellationToken)
    {
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<SlotRecurrencePreviewDto>(
            resolved, request.Body.SchoolBranchId, localizer, out _);
        if (denied is not null)
            return Result<SlotRecurrencePreviewDto>.Failure(denied.Errors, denied.ErrorCodes);
        var (occurrences, error) = InterviewAssessmentSlotSupport.Expand(request.Body);
        if (occurrences is null)
            return InterviewAssessmentSlotSupport.Fail<SlotRecurrencePreviewDto>(
                localizer, "SlotRecurrenceLimit", error!);
        var result = new List<SlotOccurrenceDto>();
        foreach (var occurrence in occurrences)
            result.Add(new(occurrence.Start, occurrence.End,
                await repository.HasConflictAsync(request.SchoolId, request.Body.SchoolBranchId,
                    request.Body.ResourceKind, request.Body.ResourceReferenceId,
                    occurrence.Start, occurrence.End, null, cancellationToken)));
        return Result<SlotRecurrencePreviewDto>.Success(new(result.Count, result));
    }
}
