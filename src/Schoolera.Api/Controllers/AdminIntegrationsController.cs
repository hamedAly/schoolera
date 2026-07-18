using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Commands.ActivateIntegration;
using Schoolera.Application.Integrations.Commands.CreateIntegration;
using Schoolera.Application.Integrations.Commands.DeactivateIntegration;
using Schoolera.Application.Integrations.Commands.SetDefaultIntegration;
using Schoolera.Application.Integrations.Commands.TestIntegrationConnection;
using Schoolera.Application.Integrations.Commands.UpdateIntegration;
using Schoolera.Application.Integrations.Commands.ValidateIntegration;
using Schoolera.Application.Integrations.Dtos;
using Schoolera.Application.Integrations.Queries.GetIntegration;
using Schoolera.Application.Integrations.Queries.ListIntegrations;
using Schoolera.Application.Integrations.Queries.ListFailedMeetingSessions;
using Schoolera.Application.Integrations.Commands.RetryMeetingSession;
using Schoolera.Application.Meetings;

namespace Schoolera.Api.Controllers;

[Route("api/admin/integrations")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminIntegrationsController(
    ISender mediator,
    ILogger<AdminIntegrationsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<IReadOnlyList<PlatformIntegrationSummaryDto>>>> List(
        [FromQuery] int? type,
        [FromQuery] string? providerCode,
        [FromQuery] bool? isActive,
        [FromQuery] int? healthStatus,
        CancellationToken cancellationToken) =>
        SendAsync(
            new ListIntegrationsQuery(type, providerCode, isActive, healthStatus),
            cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ActionResult<Result<PlatformIntegrationDetailDto>>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new GetIntegrationQuery(id), cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PlatformIntegrationDetailDto>>> Create(
        [FromBody] CreatePlatformIntegrationRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new CreateIntegrationCommand(body), cancellationToken);

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PlatformIntegrationDetailDto>>> Update(
        Guid id,
        [FromBody] UpdatePlatformIntegrationRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new UpdateIntegrationCommand(id, body), cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PlatformIntegrationDetailDto>>> Activate(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new ActivateIntegrationCommand(id), cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PlatformIntegrationDetailDto>>> Deactivate(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateIntegrationCommand(id), cancellationToken);

    [HttpPost("{id:guid}/set-default")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<PlatformIntegrationDetailDto>>> SetDefault(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new SetDefaultIntegrationCommand(id), cancellationToken);

    [HttpPost("validate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<IReadOnlyList<string>>>> Validate(
        [FromBody] ValidateIntegrationRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ValidateIntegrationCommand(null, body), cancellationToken);

    [HttpPost("{id:guid}/validate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<IReadOnlyList<string>>>> ValidateExisting(
        Guid id,
        [FromBody] ValidateIntegrationRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new ValidateIntegrationCommand(id, body), cancellationToken);

    [HttpPost("{id:guid}/test-connection")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("courier-health-check")]
    public Task<ActionResult<Result<TestConnectionResultDto>>> TestConnection(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new TestIntegrationConnectionCommand(id), cancellationToken);

    [HttpGet("meeting-sessions/failed")]
    public Task<ActionResult<Result<IReadOnlyList<FailedMeetingSessionListItemDto>>>> FailedMeetingSessions(
        [FromQuery] int take,
        CancellationToken cancellationToken) =>
        SendAsync(new ListFailedMeetingSessionsQuery(take <= 0 ? 50 : take), cancellationToken);

    [HttpPost("meeting-sessions/{meetingSessionId:guid}/retry")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<bool>>> RetryMeetingSession(
        Guid meetingSessionId,
        [FromBody] RetryMeetingSessionRequest body,
        CancellationToken cancellationToken) =>
        SendAsync(new RetryMeetingSessionCommand(meetingSessionId, body), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
