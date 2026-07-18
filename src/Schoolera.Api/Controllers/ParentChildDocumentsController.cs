using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Commands.DeleteChildDocument;
using Schoolera.Application.Parent.Commands.ReplaceChildDocument;
using Schoolera.Application.Parent.Commands.UploadChildDocument;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Parent.Queries.ListChildDocuments;

namespace Schoolera.Api.Controllers;

[Route("api/parent/children/{childId:guid}/documents")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentChildDocumentsController(
    ISender mediator,
    ILogger<ParentChildDocumentsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<ChildDocumentDto>>>> List(
        Guid childId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new ListChildDocumentsQuery(childId), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Result<ChildDocumentDto>>> Upload(
        Guid childId,
        IFormFile? file,
        [FromForm] int documentType = 1,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file?.OpenReadStream() ?? Stream.Null;
        return FromResult(await Mediator.Send(UploadChildDocumentCommand.FromRaw(
            childId, documentType, stream, file?.FileName ?? string.Empty,
            file?.ContentType ?? string.Empty, file?.Length ?? 0), cancellationToken));
    }

    [HttpPut("{documentId:guid}")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Result<ChildDocumentDto>>> Replace(
        Guid childId,
        Guid documentId,
        IFormFile? file,
        [FromForm] int documentType = 1,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file?.OpenReadStream() ?? Stream.Null;
        return FromResult(await Mediator.Send(ReplaceChildDocumentCommand.FromRaw(
            childId, documentId, documentType, stream, file?.FileName ?? string.Empty,
            file?.ContentType ?? string.Empty, file?.Length ?? 0), cancellationToken));
    }

    [HttpDelete("{documentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<bool>>> Delete(
        Guid childId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new DeleteChildDocumentCommand(childId, documentId), cancellationToken));
    }
}
