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



namespace Schoolera.Application.SchoolPortal.Commands.DeleteSchoolLogo;



public sealed record DeleteSchoolLogoCommand(Guid SchoolId) : IRequest<Result<SchoolMediaDto>>;



public sealed class DeleteSchoolLogoCommandHandler(

    ISchoolPortalAccess portalAccess,

    ISchoolPortalRepository repository,

    IFileStorage fileStorage,

    IUnitOfWork unitOfWork,

    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeleteSchoolLogoCommandHandler> logger)
    : IRequestHandler<DeleteSchoolLogoCommand, Result<SchoolMediaDto>>

{

    public async Task<Result<SchoolMediaDto>> Handle(

        DeleteSchoolLogoCommand request,

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



        var previousUrl = school.LogoUrl;

        school.SetLogoUrl(null);



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



        return Result<SchoolMediaDto>.Success(new SchoolMediaDto(null, school.CoverUrl, gallery));

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


