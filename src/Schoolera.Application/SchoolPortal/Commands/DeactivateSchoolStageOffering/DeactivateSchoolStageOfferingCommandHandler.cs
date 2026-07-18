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

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolStageOffering;

public sealed record DeactivateSchoolStageOfferingCommand(
    Guid SchoolId,
    Guid OfferingId) : IRequest<Result<SchoolStageOfferingDto>>;

public sealed class DeactivateSchoolStageOfferingCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeactivateSchoolStageOfferingCommandHandler> logger)
    : IRequestHandler<DeactivateSchoolStageOfferingCommand, Result<SchoolStageOfferingDto>>
{
    public async Task<Result<SchoolStageOfferingDto>> Handle(
        DeactivateSchoolStageOfferingCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolStageOfferingDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolStageOfferingDto>(
            accessResult.Data, SchoolPortalPermission.ManageOfferings, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var offering = await repository.GetOfferingForWriteAsync(
            request.SchoolId, request.OfferingId, cancellationToken);
        if (offering is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.OfferingNotFound);
        }

        offering.Deactivate();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolStageOfferingDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolStageOfferingDto>.Success(SchoolPortalReadModel.ToOffering(offering));
    }
}
