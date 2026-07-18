using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.DeleteSchoolCover;
using Schoolera.Application.SchoolPortal.Commands.DeleteSchoolGalleryImage;
using Schoolera.Application.SchoolPortal.Commands.DeleteSchoolLogo;
using Schoolera.Application.SchoolPortal.Commands.ReorderSchoolGalleryImages;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolGalleryImage;
using Schoolera.Application.SchoolPortal.Commands.UploadSchoolCover;
using Schoolera.Application.SchoolPortal.Commands.UploadSchoolGalleryImage;
using Schoolera.Application.SchoolPortal.Commands.UploadSchoolLogo;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolMedia;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal/schools/{schoolId:guid}/media")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalMediaController(
    ISender mediator,
    ILogger<SchoolPortalMediaController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<SchoolMediaDto>>> GetMedia(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetSchoolMediaQuery(schoolId), cancellationToken));
    }

    [HttpPost("logo")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)]
    public async Task<ActionResult<Result<SchoolMediaDto>>> UploadLogo(
        Guid schoolId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return FromResult(await Mediator.Send(new UploadSchoolLogoCommand(schoolId, stream, file.FileName, file.ContentType, file.Length), cancellationToken));
    }

    [HttpDelete("logo")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolMediaDto>>> DeleteLogo(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new DeleteSchoolLogoCommand(schoolId), cancellationToken));
    }

    [HttpPost("cover")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<Result<SchoolMediaDto>>> UploadCover(
        Guid schoolId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return FromResult(await Mediator.Send(new UploadSchoolCoverCommand(schoolId, stream, file.FileName, file.ContentType, file.Length), cancellationToken));
    }

    [HttpDelete("cover")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolMediaDto>>> DeleteCover(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new DeleteSchoolCoverCommand(schoolId), cancellationToken));
    }

    [HttpPost("gallery")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<Result<SchoolGalleryImageDto>>> UploadGalleryImage(
        Guid schoolId,
        IFormFile file,
        [FromForm] Guid? stageId,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return FromResult(await Mediator.Send(new UploadSchoolGalleryImageCommand(schoolId, stream, file.FileName, file.ContentType, file.Length, stageId), cancellationToken));
    }

    [HttpPut("gallery/{imageId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolGalleryImageDto>>> UpdateGalleryImage(
        Guid schoolId,
        Guid imageId,
        [FromBody] UpdateSchoolGalleryImageRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolGalleryImageCommand(schoolId, imageId, body),
            cancellationToken));
    }

    [HttpPut("gallery/order")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolGalleryImageDto>>>> ReorderGallery(
        Guid schoolId,
        [FromBody] ReorderSchoolGalleryImagesRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ReorderSchoolGalleryImagesCommand(schoolId, body),
            cancellationToken));
    }

    [HttpDelete("gallery/{imageId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolGalleryImageDto>>> DeleteGalleryImage(
        Guid schoolId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeleteSchoolGalleryImageCommand(schoolId, imageId),
            cancellationToken));
    }
}
