using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Queries.ListInterviewAssessmentSlotAudit;

public sealed record ListInterviewAssessmentSlotAuditQuery(Guid SchoolId, Guid SlotId)
    : IRequest<Result<IReadOnlyList<InterviewAssessmentSlotAuditDto>>>;

public sealed class ListInterviewAssessmentSlotAuditQueryHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListInterviewAssessmentSlotAuditQuery, Result<IReadOnlyList<InterviewAssessmentSlotAuditDto>>>
{
    public async Task<Result<IReadOnlyList<InterviewAssessmentSlotAuditDto>>> Handle(
        ListInterviewAssessmentSlotAuditQuery request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetAsync(request.SchoolId, request.SlotId, false, cancellationToken);
        if (slot is null)
            return InterviewAssessmentSlotSupport.Fail<IReadOnlyList<InterviewAssessmentSlotAuditDto>>(
                localizer, "SlotNotFound", SchoolPortalErrorCodes.SlotNotFound);
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<IReadOnlyList<InterviewAssessmentSlotAuditDto>>(
            resolved, slot.SchoolBranchId, localizer, out _);
        if (denied is not null)
            return Result<IReadOnlyList<InterviewAssessmentSlotAuditDto>>.Failure(
                denied.Errors, denied.ErrorCodes);
        var rows = await repository.ListAuditAsync(request.SchoolId, request.SlotId, cancellationToken);
        return Result<IReadOnlyList<InterviewAssessmentSlotAuditDto>>.Success(rows.Select(x =>
            new InterviewAssessmentSlotAuditDto(
                x.Id, x.Action, x.ActorUserId, x.Metadata, x.CreatedAtUtc)).ToArray());
    }
}
