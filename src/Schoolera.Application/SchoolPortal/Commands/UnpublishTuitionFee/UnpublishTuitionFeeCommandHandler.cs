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

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishTuitionFee;

public sealed record UnpublishTuitionFeeCommand(
    Guid SchoolId,
    Guid FeeId) : IRequest<Result<TuitionFeeDto>>;

public sealed class UnpublishTuitionFeeCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UnpublishTuitionFeeCommandHandler> logger)
    : IRequestHandler<UnpublishTuitionFeeCommand, Result<TuitionFeeDto>>
{
    public async Task<Result<TuitionFeeDto>> Handle(
        UnpublishTuitionFeeCommand request,
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

        fee.Unpublish();

        var conflict = await SchoolPortalResults.TrySaveAsync<TuitionFeeDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Unpublished tuition fee {FeeId} for school {SchoolId}.",
            fee.Id,
            request.SchoolId);
        return Result<TuitionFeeDto>.Success(SchoolPortalReadModel.ToTuitionFee(fee));
    }
}
