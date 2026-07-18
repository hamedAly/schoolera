using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Api;

/// <summary>
/// Converts a document-download <see cref="Result{T}"/> into a streamed file response with
/// hardened, non-cacheable, attachment headers, or a non-enumerating 404 for failures.
/// </summary>
public static class OnboardingDownloadResultExtensions
{
    public static IActionResult ToFileDownloadResult(
        this Result<OnboardingDocumentDownloadDto> result,
        ControllerBase controller)
    {
        if (!result.Succeeded || result.Data is null)
        {
            return controller.NotFound(result);
        }

        var download = result.Data;
        var response = controller.Response;
        response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
        response.Headers[HeaderNames.Pragma] = "no-cache";

        return new FileStreamResult(download.Content, download.ContentType)
        {
            FileDownloadName = download.DownloadFileName,
        };
    }
}
