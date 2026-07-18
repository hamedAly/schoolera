using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;

namespace Schoolera.Api;

public static class SupportTicketDownloadResultExtensions
{
    public static IActionResult ToSupportTicketFileDownloadResult(
        this Result<SupportTicketAttachmentDownloadDto> result,
        ControllerBase controller)
    {
        if (!result.Succeeded || result.Data is null)
        {
            var status = result.ErrorCodes.Contains(SupportTicketErrorCodes.NotFound) ||
                         result.ErrorCodes.Contains(SupportTicketErrorCodes.AttachmentNotFound)
                ? StatusCodes.Status404NotFound
                : result.ErrorCodes.Contains(SupportTicketErrorCodes.Forbidden)
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status400BadRequest;

            return controller.StatusCode(status, result);
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

    public static IActionResult ToSupportTicketCsvDownloadResult(
        this Result<SupportTicketExportFileDto> result,
        ControllerBase controller)
    {
        if (!result.Succeeded || result.Data is null)
        {
            var status = result.ErrorCodes.Contains(SupportTicketErrorCodes.Forbidden)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return controller.StatusCode(status, result);
        }

        var download = result.Data;
        var response = controller.Response;
        response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
        response.Headers[HeaderNames.Pragma] = "no-cache";

        return controller.File(download.Content, "text/csv", download.FileName);
    }
}
