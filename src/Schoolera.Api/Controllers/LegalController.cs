using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Legal.Dtos;
using Schoolera.Application.Legal.Queries.GetCurrentLegalDocuments;

namespace Schoolera.Api.Controllers;

[Route("api/legal")]
public sealed class LegalController(
    ISender mediator,
    ILogger<LegalController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("current")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<CurrentLegalDocumentsDto>>> GetCurrent(
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(new GetCurrentLegalDocumentsQuery(), cancellationToken));
}
