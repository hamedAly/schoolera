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



namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdditionalService;



public sealed record UpdateSchoolAdditionalServiceCommand(

    Guid SchoolId,

    Guid ServiceId,

    UpdateSchoolAdditionalServiceRequest Body) : IRequest<Result<SchoolAdditionalServiceDto>>;



public sealed class UpdateSchoolAdditionalServiceCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolAdditionalServiceCommandHandler> logger)
    : IRequestHandler<UpdateSchoolAdditionalServiceCommand, Result<SchoolAdditionalServiceDto>>

{

    public async Task<Result<SchoolAdditionalServiceDto>> Handle(

        UpdateSchoolAdditionalServiceCommand request,

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



        var body = request.Body;

        service.Update(

            body.NameAr,

            body.NameEn,

            body.DescriptionAr,

            body.DescriptionEn,

            body.IconKey,

            body.SortOrder);



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolAdditionalServiceDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            return conflict;

        }



        return Result<SchoolAdditionalServiceDto>.Success(SchoolPortalReadModel.ToService(service));

    }

}


