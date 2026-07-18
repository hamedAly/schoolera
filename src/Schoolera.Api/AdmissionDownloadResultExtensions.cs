using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Admissions.Queries.ExportAdminAdmissionApplications;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.SchoolPortal.Constants;

namespace Schoolera.Api;

public static class AdmissionDownloadResultExtensions
{
    public static IActionResult ToAdmissionFileDownloadResult(
        this Result<AdmissionAttachmentDownloadDto> result,
        ControllerBase controller)
    {
        if (!result.Succeeded || result.Data is null)
        {
            var status = result.ErrorCodes.Contains(AdmissionErrorCodes.NotFound) ||
                         result.ErrorCodes.Contains(AdmissionErrorCodes.ReviewNotFound) ||
                         result.ErrorCodes.Contains(AdmissionErrorCodes.AttachmentNotFound) ||
                         result.ErrorCodes.Contains(AdmissionErrorCodes.StudentNotOwned) ||
                         result.ErrorCodes.Contains(SchoolPortalErrorCodes.SchoolNotFound) ||
                         result.ErrorCodes.Contains(ParentErrorCodes.DocumentNotFound) ||
                         result.ErrorCodes.Contains(ParentErrorCodes.ChildNotFound)
                ? StatusCodes.Status404NotFound
                : result.ErrorCodes.Contains(AdmissionErrorCodes.Forbidden) ||
                  result.ErrorCodes.Contains(AdmissionErrorCodes.ReviewSchoolAccessDenied) ||
                  result.ErrorCodes.Contains(SchoolPortalErrorCodes.AccessDenied) ||
                  result.ErrorCodes.Contains(ParentErrorCodes.Forbidden)
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

    public static IActionResult ToAdminCsvDownloadResult(
        this Result<AdminAdmissionExportFileDto> result,
        ControllerBase controller)
    {
        if (!result.Succeeded || result.Data is null)
        {
            return controller.StatusCode(StatusCodes.Status400BadRequest, result);
        }

        var download = result.Data;
        var response = controller.Response;
        response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
        response.Headers[HeaderNames.Pragma] = "no-cache";

        return controller.File(download.Content, "text/csv", download.FileName);
    }
}
