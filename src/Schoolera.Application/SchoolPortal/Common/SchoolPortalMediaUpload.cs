using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Options;

namespace Schoolera.Application.SchoolPortal.Common;

public static class SchoolPortalMediaUpload
{
    public static async Task<Result<StoredFile>> SaveAsync(
        IFileStorage fileStorage,
        IOptions<SchoolPortalMediaOptions> mediaOptions,
        IStringLocalizer<SchoolPortalMessages> localizer,
        Stream content,
        string originalFileName,
        string contentType,
        long fileSize,
        string category,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        if (fileSize <= 0 || fileSize > maxBytes)
        {
            var code = fileSize > maxBytes
                ? SchoolPortalErrorCodes.MediaLimitExceeded
                : SchoolPortalErrorCodes.InvalidMedia;
            return SchoolPortalResults.FailureForCode<StoredFile>(localizer, code);
        }

        try
        {
            var stored = await fileStorage.SaveAsync(
                new StoreFileRequest
                {
                    Content = content,
                    OriginalFileName = originalFileName,
                    ContentType = contentType,
                    Category = category,
                },
                cancellationToken);

            return Result<StoredFile>.Success(stored);
        }
        catch (InvalidOperationException)
        {
            return SchoolPortalResults.FailureForCode<StoredFile>(
                localizer, SchoolPortalErrorCodes.InvalidMedia);
        }
    }

    public static string LogoCategory(Guid schoolId) =>
        $"{SchoolPortalMediaOptions.LogosCategory}/{schoolId:N}";

    public static string CoverCategory(Guid schoolId) =>
        $"{SchoolPortalMediaOptions.CoversCategory}/{schoolId:N}";

    public static string GalleryCategory(Guid schoolId) =>
        $"{SchoolPortalMediaOptions.GalleryCategory}/{schoolId:N}";
}
