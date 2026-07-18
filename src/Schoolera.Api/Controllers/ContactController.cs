using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Cms.Commands.SubmitContactRequest;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/contact")]
public sealed class ContactController(
    ISender mediator,
    ILogger<ContactController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpPost]
    [EnableRateLimiting("contact-submit")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<ContactRequestResultDto>>> Submit(
        [FromBody] ContactRequestBody body,
        CancellationToken cancellationToken) =>
        SendAsync(new SubmitContactRequestCommand(body), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
