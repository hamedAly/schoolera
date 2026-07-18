using MediatR;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Localization;

using Microsoft.Extensions.Options;

using Schoolera.Application.Common.Interfaces;

using Schoolera.Application.Common.Models;

using Schoolera.Application.Resources;

using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;

using Schoolera.Application.SchoolPortal.Constants;

using Schoolera.Application.SchoolPortal.Dtos;

using Schoolera.Application.SchoolPortal.Options;

using Schoolera.Domain.Entities;



namespace Schoolera.Application.SchoolPortal.Commands.UploadSchoolGalleryImage;



public sealed record UploadSchoolGalleryImageCommand(

    Guid SchoolId,

    Stream Content,

    string FileName,

    string ContentType,

    long FileSize,

    Guid? EducationalStageId) : IRequest<Result<SchoolGalleryImageDto>>;



public sealed class UploadSchoolGalleryImageCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IFileStorage fileStorage,

    IOptions<SchoolPortalMediaOptions> mediaOptions,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UploadSchoolGalleryImageCommandHandler> logger)

    : IRequestHandler<UploadSchoolGalleryImageCommand, Result<SchoolGalleryImageDto>>

{

    public async Task<Result<SchoolGalleryImageDto>> Handle(

        UploadSchoolGalleryImageCommand request,

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



        var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);

        if (school is null)

        {

            return SchoolPortalResults.FailureForCode<SchoolGalleryImageDto>(

                localizer, SchoolPortalErrorCodes.SchoolNotFound);

        }



        if (request.EducationalStageId is { } stageId &&

            !await repository.EducationalStageExistsAsync(stageId, cancellationToken))

        {

            return SchoolPortalResults.FailureForCode<SchoolGalleryImageDto>(

                localizer, SchoolPortalErrorCodes.InvalidStage);

        }



        var options = mediaOptions.Value;

        var currentCount = await repository.CountGalleryImagesAsync(

            request.SchoolId, request.EducationalStageId, cancellationToken);

        var maxCount = request.EducationalStageId is null

            ? options.MaxGeneralGalleryImages

            : options.MaxImagesPerStage;

        if (currentCount >= maxCount)

        {

            return SchoolPortalResults.FailureForCode<SchoolGalleryImageDto>(

                localizer, SchoolPortalErrorCodes.MediaLimitExceeded);

        }



        var saveResult = await SchoolPortalMediaUpload.SaveAsync(

            fileStorage,

            mediaOptions,

            localizer,

            request.Content,

            request.FileName,

            request.ContentType,

            request.FileSize,

            SchoolPortalMediaUpload.GalleryCategory(request.SchoolId),

            options.MaxGalleryImageBytes,

            cancellationToken);

        if (!saveResult.Succeeded || saveResult.Data is null)

        {

            return Result<SchoolGalleryImageDto>.Failure(saveResult.Errors, saveResult.ErrorCodes);

        }



        var image = new SchoolImage(request.SchoolId, saveResult.Data.RelativePublicUrl, currentCount);

        image.UpdateMetadata(null, null, null, null, request.EducationalStageId, currentCount);

        school.AddImage(image);



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolGalleryImageDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            await fileStorage.DeleteAsync(saveResult.Data.RelativePublicUrl, cancellationToken);

            return conflict;

        }



        return Result<SchoolGalleryImageDto>.Success(SchoolPortalReadModel.ToGalleryImage(image));

    }

}


