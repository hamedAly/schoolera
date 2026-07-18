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
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.CreateTuitionFee;

public sealed record CreateTuitionFeeCommand(
    Guid SchoolId,
    CreateTuitionFeeRequest Body) : IRequest<Result<TuitionFeeDto>>;

public sealed class CreateTuitionFeeCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateTuitionFeeCommandHandler> logger)
    : IRequestHandler<CreateTuitionFeeCommand, Result<TuitionFeeDto>>
{
    public async Task<Result<TuitionFeeDto>> Handle(
        CreateTuitionFeeCommand request,
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

        var body = request.Body;
        var branchCheck = SchoolPortalAccess.RequireBranch<TuitionFeeDto>(
            accessResult.Data, body.BranchId, localizer);
        if (!branchCheck.Succeeded)
        {
            return branchCheck;
        }

        var branch = await repository.GetBranchForWriteAsync(request.SchoolId, body.BranchId, cancellationToken);
        if (branch is null)
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.BranchNotFound);
        }

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
                body.BranchId,
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId,
                body.Category,
                null,
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
            TuitionFee.ValidateEffectiveRange(body.EffectiveFromUtc, body.EffectiveToUtc);
        }
        catch (ArgumentException)
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.InvalidFeeDateRange);
        }

        var fee = new TuitionFee(
            body.BranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId,
            body.Category,
            body.CurrencyCode,
            body.Amount,
            body.NameAr,
            body.NameEn,
            body.IsStartingFrom,
            body.SortOrder);
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

        branch.TuitionFees.Add(fee);

        var conflict = await SchoolPortalResults.TrySaveAsync<TuitionFeeDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await repository.GetTuitionFeeForWriteAsync(
            request.SchoolId, fee.Id, cancellationToken);
        if (loaded is null)
        {
            return SchoolPortalResults.FailureForCode<TuitionFeeDto>(
                localizer, SchoolPortalErrorCodes.FeeNotFound);
        }

        return Result<TuitionFeeDto>.Success(SchoolPortalReadModel.ToTuitionFee(loaded));
    }
}
