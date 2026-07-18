using MediatR;

using Microsoft.Extensions.Localization;

using Microsoft.Extensions.Logging;

using Schoolera.Application.Admissions.Common;

using Schoolera.Application.Admissions.Constants;

using Schoolera.Application.Admissions.Dtos;

using Schoolera.Application.Common.Interfaces;

using Schoolera.Application.Common.Models;

using Schoolera.Application.Resources;

using Schoolera.Application.SchoolPortal.Auth;

using Schoolera.Application.SchoolPortal.Common;



namespace Schoolera.Application.Admissions.Queries.DownloadSchoolAdmissionAttachment;



public sealed record DownloadSchoolAdmissionAttachmentQuery(

    Guid SchoolId,

    Guid ApplicationId,

    Guid AttachmentId) : IRequest<Result<AdmissionAttachmentDownloadDto>>;



public sealed class DownloadSchoolAdmissionAttachmentQueryHandler(

    ISchoolPortalAccess portalAccess,

    IAdmissionApplicationRepository admissionRepository,

    IPrivateFileStorage privateFileStorage,

    IStringLocalizer<SchoolPortalMessages> localizer,

    ILogger<DownloadSchoolAdmissionAttachmentQueryHandler> logger)

    : IRequestHandler<DownloadSchoolAdmissionAttachmentQuery, Result<AdmissionAttachmentDownloadDto>>

{

    public async Task<Result<AdmissionAttachmentDownloadDto>> Handle(

        DownloadSchoolAdmissionAttachmentQuery request,

        CancellationToken cancellationToken)

    {

        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);

        if (!accessResult.Succeeded || accessResult.Data is null)

        {

            return Result<AdmissionAttachmentDownloadDto>.Failure(

                accessResult.Errors,

                accessResult.ErrorCodes);

        }



        var access = accessResult.Data;

        var permissionCheck = SchoolPortalAccess.RequirePermission<AdmissionAttachmentDownloadDto>(

            access, SchoolPortalPermission.DownloadApplicationAttachments, localizer);

        if (!permissionCheck.Succeeded)

        {

            return permissionCheck;

        }



        var attachment = await admissionRepository.GetSchoolAttachmentAsync(

            request.SchoolId,

            request.ApplicationId,

            request.AttachmentId,

            cancellationToken);



        // Same safe 404 for missing applications/attachments and wrong-school rows.

        if (attachment is null)

        {

            return AdmissionResults.Failure<AdmissionAttachmentDownloadDto>(

                "Attachment not found.",

                AdmissionErrorCodes.AttachmentNotFound);

        }



        var branchCheck = SchoolPortalAccess.RequireBranch<AdmissionAttachmentDownloadDto>(

            access, attachment.AdmissionApplication.SchoolBranchId, localizer);

        if (!branchCheck.Succeeded)

        {

            return branchCheck;

        }



        var stream = await privateFileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);

        if (stream is null)

        {

            logger.LogWarning(

                "Stored file missing for school admission attachment {AttachmentId}.",

                attachment.Id);

            return AdmissionResults.Failure<AdmissionAttachmentDownloadDto>(

                "Attachment not found.",

                AdmissionErrorCodes.AttachmentNotFound);

        }



        return Result<AdmissionAttachmentDownloadDto>.Success(

            new AdmissionAttachmentDownloadDto(

                stream,

                attachment.ContentType,

                AdmissionMapping.SafeOriginalFileName(attachment.OriginalFileName),

                attachment.FileSizeBytes));

    }

}


