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

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolPublishedDiscount;

public sealed record UpdateSchoolPublishedDiscountCommand(
    Guid SchoolId,
    Guid DiscountId,
    UpdateSchoolPublishedDiscountRequest Body) : IRequest<Result<SchoolPublishedDiscountDto>>;

public sealed class UpdateSchoolPublishedDiscountCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolPublishedDiscountCommandHandler> logger)
    : IRequestHandler<UpdateSchoolPublishedDiscountCommand, Result<SchoolPublishedDiscountDto>>
{
    public async Task<Result<SchoolPublishedDiscountDto>> Handle(
        UpdateSchoolPublishedDiscountCommand request,
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

        var discount = await repository.GetPublishedDiscountForWriteAsync(
            request.SchoolId, request.DiscountId, cancellationToken);
        if (discount is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                localizer, SchoolPortalErrorCodes.DiscountNotFound);
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

        try
        {
            discount.Update(
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

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolPublishedDiscountDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolPublishedDiscountDto>.Success(
            SchoolPortalReadModel.ToDiscount(discount, DateTimeOffset.UtcNow));
    }
}
