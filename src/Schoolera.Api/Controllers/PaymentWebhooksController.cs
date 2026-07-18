using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Payments.Commands.ProcessSandboxFinancingWebhook;
using Schoolera.Application.Payments.Commands.ProcessSandboxPaymentWebhook;
using Schoolera.Application.Payments.Dtos;

namespace Schoolera.Api.Controllers;

/// <summary>
/// Sandbox provider callbacks — no cookie auth, no CSRF. Signature verified in handlers.
/// </summary>
[Route("api/webhooks/payments")]
[AllowAnonymous]
public sealed class PaymentWebhooksController(
    ISender mediator,
    ILogger<PaymentWebhooksController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpPost("schoolera-sandbox")]
    public Task<ActionResult<Result<SandboxWebhookAckDto>>> SchooleraSandbox(
        CancellationToken cancellationToken) =>
        SendAsync(cancellationToken);

    private async Task<ActionResult<Result<SandboxWebhookAckDto>>> SendAsync(
        CancellationToken cancellationToken)
    {
        var rawBody = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync(cancellationToken);
        return FromResult(await Mediator.Send(
            new ProcessSandboxPaymentWebhookCommand(
                rawBody,
                Request.Headers["X-Schoolera-Signature"].FirstOrDefault(),
                Request.Headers["X-Schoolera-Timestamp"].FirstOrDefault()),
            cancellationToken));
    }
}

[Route("api/webhooks/financing")]
[AllowAnonymous]
public sealed class FinancingWebhooksController(
    ISender mediator,
    ILogger<FinancingWebhooksController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpPost("schoolera-sandbox")]
    public Task<ActionResult<Result<SandboxWebhookAckDto>>> SchooleraSandbox(
        CancellationToken cancellationToken) =>
        SendAsync(cancellationToken);

    private async Task<ActionResult<Result<SandboxWebhookAckDto>>> SendAsync(
        CancellationToken cancellationToken)
    {
        var rawBody = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync(cancellationToken);
        return FromResult(await Mediator.Send(
            new ProcessSandboxFinancingWebhookCommand(
                rawBody,
                Request.Headers["X-Schoolera-Signature"].FirstOrDefault(),
                Request.Headers["X-Schoolera-Timestamp"].FirstOrDefault()),
            cancellationToken));
    }
}
