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

namespace Schoolera.Application.SchoolPortal.Commands.CloseInterviewAssessmentSlot;

public sealed record CloseInterviewAssessmentSlotCommand(
    Guid SchoolId, Guid SlotId, byte[]? RowVersion) : IRequest<Result<InterviewAssessmentSlotDto>>;

public sealed class CloseInterviewAssessmentSlotCommandHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CloseInterviewAssessmentSlotCommandHandler> logger)
    : IRequestHandler<CloseInterviewAssessmentSlotCommand, Result<InterviewAssessmentSlotDto>>
{
    public async Task<Result<InterviewAssessmentSlotDto>> Handle(
        CloseInterviewAssessmentSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetAsync(request.SchoolId, request.SlotId, true, cancellationToken);
        if (slot is null)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotNotFound", SchoolPortalErrorCodes.SlotNotFound);
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<InterviewAssessmentSlotDto>(
            resolved, slot.SchoolBranchId, localizer, out var access);
        if (denied is not null || access is null)
            return Result<InterviewAssessmentSlotDto>.Failure(denied!.Errors, denied.ErrorCodes);
        if (InterviewAssessmentSlotSupport.RowVersionMismatch(request.RowVersion, slot.RowVersion))
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "ConcurrentUpdate", SchoolPortalErrorCodes.ConcurrentUpdate);
        try
        {
            slot.Close(access.UserId);
        }
        catch
        {
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidTransition", SchoolPortalErrorCodes.SlotInvalidTransition);
        }
        await repository.AddAuditAsync(
            new(request.SchoolId, slot.Id, "Closed", access.UserId, null), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Closed interview assessment slot {SlotId}.", slot.Id);
        return Result<InterviewAssessmentSlotDto>.Success(
            await InterviewAssessmentSlotSupport.MapAsync(slot, repository, cancellationToken));
    }
}
