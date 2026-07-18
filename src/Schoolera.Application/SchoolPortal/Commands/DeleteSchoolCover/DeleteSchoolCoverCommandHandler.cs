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



namespace Schoolera.Application.SchoolPortal.Commands.DeleteSchoolCover;



public sealed record DeleteSchoolCoverCommand(Guid SchoolId) : IRequest<Result<SchoolMediaDto>>;



public sealed class DeleteSchoolCoverCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IFileStorage fileStorage,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeleteSchoolCoverCommandHandler> logger)
    : IRequestHandler<DeleteSchoolCoverCommand, Result<SchoolMediaDto>>

{

    public async Task<Result<SchoolMediaDto>> Handle(

        DeleteSchoolCoverCommand request,

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



        var previousUrl = school.CoverUrl;

        school.SetCoverUrl(null);



        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolMediaDto>(

            unitOfWork, localizer, cancellationToken);

        if (conflict is not null)

        {

            return conflict;

        }



        await TryDeleteUnreferencedUrlAsync(previousUrl, cancellationToken);



        var images = await repository.ListImagesAsync(request.SchoolId, cancellationToken);

        var gallery = images

            .Where(image => image.IsActive)

            .Select(SchoolPortalReadModel.ToGalleryImage)

            .ToArray();



        return Result<SchoolMediaDto>.Success(new SchoolMediaDto(school.LogoUrl, null, gallery));

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


