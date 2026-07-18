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

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolPublishedDiscount;

public sealed record UnpublishSchoolPublishedDiscountCommand(
    Guid SchoolId,
    Guid ItemId) : IRequest<Result<SchoolPublishedDiscountDto>>;

public sealed class UnpublishSchoolPublishedDiscountCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UnpublishSchoolPublishedDiscountCommandHandler> logger)
    : IRequestHandler<UnpublishSchoolPublishedDiscountCommand, Result<SchoolPublishedDiscountDto>>
{
    public async Task<Result<SchoolPublishedDiscountDto>> Handle(
        UnpublishSchoolPublishedDiscountCommand request,
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

        var entity = await repository.GetPublishedDiscountForWriteAsync(request.SchoolId, request.ItemId, cancellationToken);
        if (entity is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolPublishedDiscountDto>(
                localizer, SchoolPortalErrorCodes.DiscountNotFound);
        }

        entity.Unpublish();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolPublishedDiscountDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolPublishedDiscountDto>.Success(SchoolPortalReadModel.ToDiscount(entity, DateTimeOffset.UtcNow));
    }
}
