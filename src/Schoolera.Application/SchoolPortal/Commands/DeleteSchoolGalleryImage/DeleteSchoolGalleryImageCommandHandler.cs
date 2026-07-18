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



namespace Schoolera.Application.SchoolPortal.Commands.DeleteSchoolGalleryImage;



public sealed record DeleteSchoolGalleryImageCommand(Guid SchoolId, Guid ImageId)

    : IRequest<Result<SchoolGalleryImageDto>>;



public sealed class DeleteSchoolGalleryImageCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IFileStorage fileStorage,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeleteSchoolGalleryImageCommandHandler> logger)
    : IRequestHandler<DeleteSchoolGalleryImageCommand, Result<SchoolGalleryImageDto>>

{

    public async Task<Result<SchoolGalleryImageDto>> Handle(

        DeleteSchoolGalleryImageCommand request,

        CancellationToken cancellationToken)

    {

        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);

        if (!accessResult.Succeeded || accessResult.Data is null)

        {

            return Result<SchoolGalleryImageDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);

        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolGalleryImageDto>(
            accessResult.Data, SchoolPortalPermission.ManageGallery, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }



        var image = await repository.GetImageForWriteAsync(request.SchoolId, request.ImageId, cancellationToken);

        if (image is null)

        {

            return SchoolPortalResults.FailureForCode<SchoolGalleryImageDto>(

                localizer, SchoolPortalErrorCodes.ImageNotFound);

        }



        var imageUrl = image.ImageUrl;

        image.Deactivate();



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolGalleryImageDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            return conflict;

        }



        var referenced = await repository.GetReferencedMediaUrlsAsync(cancellationToken);

        if (!referenced.Contains(imageUrl))

        {

            await fileStorage.DeleteAsync(imageUrl, cancellationToken);

        }



        return Result<SchoolGalleryImageDto>.Success(SchoolPortalReadModel.ToGalleryImage(image));

    }

}


