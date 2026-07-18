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



namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdditionalService;



public sealed record CreateSchoolAdditionalServiceCommand(

    Guid SchoolId,

    CreateSchoolAdditionalServiceRequest Body) : IRequest<Result<SchoolAdditionalServiceDto>>;



public sealed class CreateSchoolAdditionalServiceCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolAdditionalServiceCommandHandler> logger)
    : IRequestHandler<CreateSchoolAdditionalServiceCommand, Result<SchoolAdditionalServiceDto>>

{

    public async Task<Result<SchoolAdditionalServiceDto>> Handle(

        CreateSchoolAdditionalServiceCommand request,

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



        var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);

        if (school is null)

        {

            return SchoolPortalResults.FailureForCode<SchoolAdditionalServiceDto>(

                localizer, SchoolPortalErrorCodes.SchoolNotFound);

        }



        var body = request.Body;

        var service = new SchoolAdditionalService(

            request.SchoolId,

            body.NameAr,

            body.NameEn,

            body.DescriptionAr,

            body.DescriptionEn,

            body.IconKey,

            body.SortOrder);



        school.AddAdditionalService(service);



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolAdditionalServiceDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            return conflict;

        }



        var loaded = await repository.GetServiceForWriteAsync(request.SchoolId, service.Id, cancellationToken);

        if (loaded is null)

        {

            return SchoolPortalResults.FailureForCode<SchoolAdditionalServiceDto>(

                localizer, SchoolPortalErrorCodes.ServiceNotFound);

        }



        return Result<SchoolAdditionalServiceDto>.Success(SchoolPortalReadModel.ToService(loaded));

    }

}


