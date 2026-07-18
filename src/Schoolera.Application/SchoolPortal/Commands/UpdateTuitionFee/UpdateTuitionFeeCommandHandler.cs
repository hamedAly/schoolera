using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateTuitionFee;

public sealed record UpdateTuitionFeeCommand(
    Guid SchoolId,
    Guid FeeId,
    UpdateTuitionFeeRequest Body) : IRequest<Result<TuitionFeeDto>>;

public sealed class UpdateTuitionFeeCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateTuitionFeeCommandHandler> logger)
    : IRequestHandler<UpdateTuitionFeeCommand, Result<TuitionFeeDto>>
{
    public async Task<Result<TuitionFeeDto>> Handle(
        UpdateTuitionFeeCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<TuitionFeeDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<TuitionFeeDto>(
            accessResult.Data, SchoolPortalPermission.ManageFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var fee = await repository.GetTuitionFeeForWriteAsync(
            request.SchoolId, request.FeeId, cancellationToken);
        if (fee is null)
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.FeeNotFound);
        }

        var branchCheck = SchoolPortalAccess.RequireBranch<TuitionFeeDto>(
            accessResult.Data, fee.SchoolBranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        var body = request.Body;

        if (!await repository.EducationalStageExistsAsync(body.EducationalStageId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.InvalidStage);
        }

        if (body.GradeId is { } gradeId &&
            !await repository.GradesBelongToStageAsync(
                body.EducationalStageId, [gradeId], cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.InvalidGrade);
        }

        if (!await repository.AcademicYearExistsAsync(body.AcademicYearId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.InvalidAcademicYear);
        }

        if (await repository.DuplicateFeeExistsAsync(
                fee.SchoolBranchId,
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId,
                body.Category,
                fee.Id,
                cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.DuplicateFee);
        }

        if (body.Amount < 0m || !Enum.IsDefined(body.Category))
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.InvalidFee);
        }

        try
        {
            fee.Update(
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId,
                body.Category,
                body.NameAr,
                body.NameEn,
                body.CurrencyCode,
                body.Amount,
                body.IsStartingFrom,
                body.NotesAr,
                body.NotesEn,
                body.InternalNotesAr,
                body.InternalNotesEn,
                body.SortOrder,
                body.EffectiveFromUtc,
                body.EffectiveToUtc);
        }
        catch (ArgumentException)
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.InvalidFeeDateRange);
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<TuitionFeeDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<TuitionFeeDto>.Success(SchoolPortalReadModel.ToTuitionFee(fee));
    }
}
