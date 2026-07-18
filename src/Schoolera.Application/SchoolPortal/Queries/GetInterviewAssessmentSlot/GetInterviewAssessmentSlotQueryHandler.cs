using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.GetInterviewAssessmentSlot;

public sealed record GetInterviewAssessmentSlotQuery(Guid SchoolId, Guid SlotId)
    : IRequest<Result<InterviewAssessmentSlotDto>>;

public sealed class GetInterviewAssessmentSlotQueryHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<GetInterviewAssessmentSlotQuery, Result<InterviewAssessmentSlotDto>>
{
    public async Task<Result<InterviewAssessmentSlotDto>> Handle(
        GetInterviewAssessmentSlotQuery request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetAsync(request.SchoolId, request.SlotId, false, cancellationToken);
        if (slot is null)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotNotFound", SchoolPortalErrorCodes.SlotNotFound);
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<InterviewAssessmentSlotDto>(
            resolved, slot.SchoolBranchId, localizer, out _);
        if (denied is not null)
            return Result<InterviewAssessmentSlotDto>.Failure(denied.Errors, denied.ErrorCodes);
        return Result<InterviewAssessmentSlotDto>.Success(
            await InterviewAssessmentSlotSupport.MapAsync(slot, repository, cancellationToken));
    }
}
