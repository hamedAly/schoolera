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
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.ReopenInterviewAssessmentSlot;

public sealed record ReopenInterviewAssessmentSlotCommand(
    Guid SchoolId, Guid SlotId, byte[]? RowVersion) : IRequest<Result<InterviewAssessmentSlotDto>>;

public sealed class ReopenInterviewAssessmentSlotCommandHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    ISchoolInterviewAssessmentPolicyRepository policies,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ReopenInterviewAssessmentSlotCommandHandler> logger)
    : IRequestHandler<ReopenInterviewAssessmentSlotCommand, Result<InterviewAssessmentSlotDto>>
{
    public async Task<Result<InterviewAssessmentSlotDto>> Handle(
        ReopenInterviewAssessmentSlotCommand request, CancellationToken cancellationToken)
    {
        _ = unitOfWork;
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
        var code = await InterviewAssessmentSlotSupport.ValidateOpenAsync(
            slot, repository, policies, cancellationToken);
        if (code is not null)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotPolicyMismatch", code);
        var auditAction = slot.Status == SlotStatus.Closed ? "Reopened" : "Opened";
        var openResult = await repository.ExecuteAtomicOpenAsync(
            request.SchoolId, slot.Id, slot.ResourceKind, slot.ResourceReferenceId,
            slot.StartAtUtc, slot.EndAtUtc,
            async token =>
            {
                slot.Open(access.UserId);
                await repository.AddAuditAsync(
                    new(request.SchoolId, slot.Id, auditAction, access.UserId, null), token);
            }, cancellationToken);
        if (openResult == AtomicSlotOpenResult.ResourceConflict)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotPolicyMismatch", SchoolPortalErrorCodes.SlotResourceConflict);
        if (openResult != AtomicSlotOpenResult.Succeeded)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "ConcurrentUpdate", SchoolPortalErrorCodes.ConcurrentUpdate);
        logger.LogInformation("Reopened interview assessment slot {SlotId}.", slot.Id);
        return Result<InterviewAssessmentSlotDto>.Success(
            await InterviewAssessmentSlotSupport.MapAsync(slot, repository, cancellationToken));
    }
}
