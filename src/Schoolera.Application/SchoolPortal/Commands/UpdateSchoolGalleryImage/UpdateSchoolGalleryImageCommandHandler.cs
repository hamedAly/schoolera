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



namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolGalleryImage;



public sealed record UpdateSchoolGalleryImageCommand(

    Guid SchoolId,

    Guid ImageId,

    UpdateSchoolGalleryImageRequest Body) : IRequest<Result<SchoolGalleryImageDto>>;



public sealed class UpdateSchoolGalleryImageCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolGalleryImageCommandHandler> logger)
    : IRequestHandler<UpdateSchoolGalleryImageCommand, Result<SchoolGalleryImageDto>>

{

    public async Task<Result<SchoolGalleryImageDto>> Handle(

        UpdateSchoolGalleryImageCommand request,

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



        var body = request.Body;

        if (body.EducationalStageId is { } stageId &&

            !await repository.EducationalStageExistsAsync(stageId, cancellationToken))

        {

            return SchoolPortalResults.FailureForCode<SchoolGalleryImageDto>(

                localizer, SchoolPortalErrorCodes.InvalidStage);

        }



        image.UpdateMetadata(

            body.CaptionAr,

            body.CaptionEn,

            body.AltTextAr,

            body.AltTextEn,

            body.EducationalStageId,

            body.SortOrder);



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolGalleryImageDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            return conflict;

        }



        return Result<SchoolGalleryImageDto>.Success(SchoolPortalReadModel.ToGalleryImage(image));

    }

}


