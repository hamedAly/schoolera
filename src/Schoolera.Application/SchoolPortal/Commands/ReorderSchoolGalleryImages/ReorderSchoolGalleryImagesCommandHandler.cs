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

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolGalleryImages;

public sealed record ReorderSchoolGalleryImagesCommand(
    Guid SchoolId,
    ReorderSchoolGalleryImagesRequest Body) : IRequest<Result<IReadOnlyList<SchoolGalleryImageDto>>>;

public sealed class ReorderSchoolGalleryImagesCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<ReorderSchoolGalleryImagesCommandHandler> logger)
    : IRequestHandler<ReorderSchoolGalleryImagesCommand, Result<IReadOnlyList<SchoolGalleryImageDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolGalleryImageDto>>> Handle(
        ReorderSchoolGalleryImagesCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<IReadOnlyList<SchoolGalleryImageDto>>.Failure(
                accessResult.Errors, accessResult.ErrorCodes);
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<IReadOnlyList<SchoolGalleryImageDto>>(
            accessResult.Data, SchoolPortalPermission.ManageGallery, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var images = (await repository.ListImagesAsync(request.SchoolId, cancellationToken))
            .Where(image => image.IsActive)
            .ToArray();

        var imageIds = request.Body.ImageIds;
        if (imageIds.Count != images.Length ||
            images.Select(image => image.Id).ToHashSet().SetEquals(imageIds) == false)
        {
            return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolGalleryImageDto>>(
                localizer, SchoolPortalErrorCodes.InvalidReorder);
        }

        var imagesById = images.ToDictionary(image => image.Id);
        for (var index = 0; index < imageIds.Count; index++)
        {
            var tracked = await repository.GetImageForWriteAsync(
                request.SchoolId, imageIds[index], cancellationToken);
            if (tracked is null)
            {
                return SchoolPortalResults.FailureForCode<IReadOnlyList<SchoolGalleryImageDto>>(
                    localizer, SchoolPortalErrorCodes.ImageNotFound);
            }

            tracked.SetSortOrder(index);
            imagesById[tracked.Id] = tracked;
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<IReadOnlyList<SchoolGalleryImageDto>>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var ordered = imageIds
            .Select(id => SchoolPortalReadModel.ToGalleryImage(imagesById[id]))
            .ToArray();

        return Result<IReadOnlyList<SchoolGalleryImageDto>>.Success(ordered);
    }
}
