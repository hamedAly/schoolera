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

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFeeVisibility;

public sealed record UpdateSchoolFeeVisibilityCommand(
    Guid SchoolId,
    UpdateSchoolFeeVisibilityRequest Body) : IRequest<Result<SchoolFeeVisibilityDto>>;

public sealed class UpdateSchoolFeeVisibilityCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolFeeVisibilityCommandHandler> logger)
    : IRequestHandler<UpdateSchoolFeeVisibilityCommand, Result<SchoolFeeVisibilityDto>>
{
    public async Task<Result<SchoolFeeVisibilityDto>> Handle(
        UpdateSchoolFeeVisibilityCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolFeeVisibilityDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolFeeVisibilityDto>(
            accessResult.Data, SchoolPortalPermission.ManageFees, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeVisibilityDto>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        var policy = request.Body.FeeVisibilityPolicy;
        if (policy is { } value && !Enum.IsDefined(value))
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeVisibilityDto>(
                localizer, SchoolPortalErrorCodes.InvalidFeeVisibility);
        }

        try
        {
            school.SetFeeVisibilityPolicy(policy);
        }
        catch (ArgumentOutOfRangeException)
        {
            return SchoolPortalResults.FailureForCode<SchoolFeeVisibilityDto>(
                localizer, SchoolPortalErrorCodes.InvalidFeeVisibility);
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolFeeVisibilityDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Updated fee visibility for school {SchoolId} to {Policy}.",
            request.SchoolId,
            policy);
        return Result<SchoolFeeVisibilityDto>.Success(new SchoolFeeVisibilityDto(school.FeeVisibilityPolicy));
    }
}
