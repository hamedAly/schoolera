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
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolPublishedDiscount;

public sealed record CreateSchoolPublishedDiscountCommand(
    Guid SchoolId,
    CreateSchoolPublishedDiscountRequest Body) : IRequest<Result<SchoolPublishedDiscountDto>>;

public sealed class CreateSchoolPublishedDiscountCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolPublishedDiscountCommandHandler> logger)
    : IRequestHandler<CreateSchoolPublishedDiscountCommand, Result<SchoolPublishedDiscountDto>>
{
    public async Task<Result<SchoolPublishedDiscountDto>> Handle(
        CreateSchoolPublishedDiscountCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolPublishedDiscountDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolPublishedDiscountDto>(
            accessResult.Data, SchoolPortalPermission.ManageFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var body = request.Body;
        if (body.BranchId is { } branchId)
        {
            var branchCheck = SchoolPortalAccess.RequireBranch<SchoolPublishedDiscountDto>(
                accessResult.Data, branchId, localizer);
            if (!branchCheck.Succeeded)
            {
                return branchCheck;
            }

            var branch = await repository.GetBranchForWriteAsync(
                request.SchoolId, branchId, cancellationToken);
            if (branch is null)
            {
                return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                    localizer, SchoolPortalErrorCodes.BranchNotFound);
            }
        }

        if (body.EducationalStageId is { } stageId &&
            !await repository.EducationalStageExistsAsync(stageId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                localizer, SchoolPortalErrorCodes.InvalidStage);
        }

        if (body.GradeId is { } gradeId)
        {
            if (body.EducationalStageId is null ||
                !await repository.GradesBelongToStageAsync(
                    body.EducationalStageId.Value, [gradeId], cancellationToken))
            {
                return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                    localizer, SchoolPortalErrorCodes.InvalidGrade);
            }
        }

        if (body.AcademicYearId is { } yearId &&
            !await repository.AcademicYearExistsAsync(yearId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                localizer, SchoolPortalErrorCodes.InvalidAcademicYear);
        }

        SchoolPublishedDiscount discount;
        try
        {
            discount = new SchoolPublishedDiscount(
                request.SchoolId,
                body.TitleAr,
                body.TitleEn,
                body.EligibilityDescriptionAr,
                body.EligibilityDescriptionEn,
                body.DiscountType,
                body.Value,
                body.CurrencyCode,
                body.StartUtc,
                body.EndUtc,
                body.BranchId,
                body.EducationalStageId,
                body.GradeId,
                body.AcademicYearId,
                body.SortOrder);
        }
        catch (ArgumentException)
        {
            return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                localizer, SchoolPortalErrorCodes.InvalidDiscount);
        }

        await repository.AddPublishedDiscountAsync(discount, cancellationToken);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolPublishedDiscountDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Created published discount {DiscountId} for school {SchoolId}.",
            discount.Id,
            request.SchoolId);
        return Result<SchoolPublishedDiscountDto>.Success(
            SchoolPortalReadModel.ToDiscount(discount, DateTimeOffset.UtcNow));
    }
}
