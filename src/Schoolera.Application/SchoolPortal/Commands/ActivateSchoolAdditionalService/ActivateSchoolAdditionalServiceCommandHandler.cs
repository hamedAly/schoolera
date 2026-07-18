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



namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolAdditionalService;



public sealed record ActivateSchoolAdditionalServiceCommand(Guid SchoolId, Guid ServiceId)

    : IRequest<Result<SchoolAdditionalServiceDto>>;



public sealed class ActivateSchoolAdditionalServiceCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ActivateSchoolAdditionalServiceCommandHandler> logger)
    : IRequestHandler<ActivateSchoolAdditionalServiceCommand, Result<SchoolAdditionalServiceDto>>

{

    public async Task<Result<SchoolAdditionalServiceDto>> Handle(

        ActivateSchoolAdditionalServiceCommand request,

        CancellationToken cancellationToken)

    {

        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);

        if (!accessResult.Succeeded || accessResult.Data is null)

        {

            return Result<SchoolAdditionalServiceDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);

        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolAdditionalServiceDto>(
            accessResult.Data, SchoolPortalPermission.ManageServices, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }



        var service = await repository.GetServiceForWriteAsync(

            request.SchoolId, request.ServiceId, cancellationToken);

        if (service is null)

        {

            return SchoolPortalResults.FailureForCode<SchoolAdditionalServiceDto>(

                localizer, SchoolPortalErrorCodes.ServiceNotFound);

        }



        service.Activate();



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolAdditionalServiceDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            return conflict;

        }



        return Result<SchoolAdditionalServiceDto>.Success(SchoolPortalReadModel.ToService(service));

    }

}


