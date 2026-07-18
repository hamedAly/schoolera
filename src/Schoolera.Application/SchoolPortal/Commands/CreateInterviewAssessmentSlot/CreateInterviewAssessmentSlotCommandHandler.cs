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

namespace Schoolera.Application.SchoolPortal.Commands.CreateInterviewAssessmentSlot;

public sealed record CreateInterviewAssessmentSlotCommand(
    Guid SchoolId, UpsertInterviewAssessmentSlotRequest Body)
    : IRequest<Result<InterviewAssessmentSlotDto>>;

public sealed class CreateInterviewAssessmentSlotCommandHandler(
    ISchoolPortalAccess accessService,
    IInterviewAssessmentSlotRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateInterviewAssessmentSlotCommandHandler> logger)
    : IRequestHandler<CreateInterviewAssessmentSlotCommand, Result<InterviewAssessmentSlotDto>>
{
    public async Task<Result<InterviewAssessmentSlotDto>> Handle(
        CreateInterviewAssessmentSlotCommand request, CancellationToken cancellationToken)
    {
        var resolved = await accessService.ResolveAsync(request.SchoolId, cancellationToken);
        var denied = InterviewAssessmentSlotSupport.Authorize<InterviewAssessmentSlotDto>(
            resolved, request.Body.SchoolBranchId, localizer, out var access);
        if (denied is not null || access is null)
            return Result<InterviewAssessmentSlotDto>.Failure(denied!.Errors, denied.ErrorCodes);
        if (!await repository.BranchScopeIsValidAsync(request.SchoolId, request.Body.SchoolBranchId,
                request.Body.EducationalStageId, request.Body.GradeId,
                request.Body.AcademicYearId, cancellationToken))
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidScope", SchoolPortalErrorCodes.SlotInvalidScope);
        if (request.Body.LocalEndTime <= request.Body.LocalStartTime)
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidTime", SchoolPortalErrorCodes.SlotInvalidTime);
        if (!InterviewAssessmentSlotSupport.TryLocalToUtc(
                request.Body.LocalDate, request.Body.LocalStartTime, request.Body.TimeZoneId,
                out var startAtUtc, out var conversionError) ||
            !InterviewAssessmentSlotSupport.TryLocalToUtc(
                request.Body.LocalDate, request.Body.LocalEndTime, request.Body.TimeZoneId,
                out var endAtUtc, out conversionError))
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidTime", conversionError!);
        InterviewAssessmentSlot slot;
        try
        {
            slot = new(request.SchoolId, request.Body.SchoolBranchId,
                request.Body.EducationalStageId, request.Body.GradeId, request.Body.AcademicYearId,
                request.Body.Kind, request.Body.DeliveryMode, startAtUtc, endAtUtc,
                request.Body.TimeZoneId, request.Body.Capacity, request.Body.ResourceKind,
                request.Body.ResourceReferenceId, request.Body.InstructionsAr,
                request.Body.InstructionsEn, request.Body.MeetingProviderCode, null, access.UserId);
        }
        catch
        {
            return InterviewAssessmentSlotSupport.Fail<InterviewAssessmentSlotDto>(
                localizer, "SlotInvalidTime", SchoolPortalErrorCodes.SlotInvalidTime);
        }
        await repository.AddAsync(slot, cancellationToken);
        await repository.AddAuditAsync(
            new(request.SchoolId, slot.Id, "Created", access.UserId, null), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created interview assessment slot {SlotId}.", slot.Id);
        return Result<InterviewAssessmentSlotDto>.Success(
            await InterviewAssessmentSlotSupport.MapAsync(slot, repository, cancellationToken));
    }
}
