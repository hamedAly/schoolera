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



namespace Schoolera.Application.SchoolPortal.Commands.UploadSchoolCover;



public sealed record UploadSchoolCoverCommand(

    Guid SchoolId,

    Stream Content,

    string FileName,

    string ContentType,

    long FileSize) : IRequest<Result<SchoolMediaDto>>;



public sealed class UploadSchoolCoverCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IFileStorage fileStorage,

    IOptions<SchoolPortalMediaOptions> mediaOptions,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UploadSchoolCoverCommandHandler> logger)

    : IRequestHandler<UploadSchoolCoverCommand, Result<SchoolMediaDto>>

{

    public async Task<Result<SchoolMediaDto>> Handle(

        UploadSchoolCoverCommand request,

        CancellationToken cancellationToken)

    {

        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);

        if (!accessResult.Succeeded || accessResult.Data is null)

        {

            return Result<SchoolMediaDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);

        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolMediaDto>(
            accessResult.Data, SchoolPortalPermission.ManageGallery, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }



        var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);

        if (school is null)

        {

            return SchoolPortalResults.FailureForCode<SchoolMediaDto>(

                localizer, SchoolPortalErrorCodes.SchoolNotFound);

        }



        var options = mediaOptions.Value;

        var saveResult = await SchoolPortalMediaUpload.SaveAsync(

            fileStorage,

            mediaOptions,

            localizer,

            request.Content,

            request.FileName,

            request.ContentType,

            request.FileSize,

            SchoolPortalMediaUpload.CoverCategory(request.SchoolId),

            options.MaxCoverBytes,

            cancellationToken);

        if (!saveResult.Succeeded || saveResult.Data is null)

        {

            return Result<SchoolMediaDto>.Failure(saveResult.Errors, saveResult.ErrorCodes);

        }



        var previousUrl = school.CoverUrl;

        school.SetCoverUrl(saveResult.Data.RelativePublicUrl);



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolMediaDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            await fileStorage.DeleteAsync(saveResult.Data.RelativePublicUrl, cancellationToken);

            return conflict;

        }



        await TryDeleteUnreferencedUrlAsync(previousUrl, cancellationToken);



        var images = await repository.ListImagesAsync(request.SchoolId, cancellationToken);

        var gallery = images

            .Where(image => image.IsActive)

            .Select(SchoolPortalReadModel.ToGalleryImage)

            .ToArray();



        return Result<SchoolMediaDto>.Success(new SchoolMediaDto(school.LogoUrl, school.CoverUrl, gallery));

    }



    private async Task TryDeleteUnreferencedUrlAsync(string? url, CancellationToken cancellationToken)

    {

        if (string.IsNullOrWhiteSpace(url))

        {

            return;

        }



        var referenced = await repository.GetReferencedMediaUrlsAsync(cancellationToken);

        if (!referenced.Contains(url))

        {

            await fileStorage.DeleteAsync(url, cancellationToken);

        }

    }

}


