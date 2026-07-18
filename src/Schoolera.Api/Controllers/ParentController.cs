using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Commands.CreateChildProfile;
using Schoolera.Application.Parent.Commands.DeleteChildProfile;
using Schoolera.Application.Parent.Commands.UpdateChildProfile;
using Schoolera.Application.Parent.Commands.UpdateParentProfile;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Parent.Queries.GetParentChild;
using Schoolera.Application.Parent.Queries.GetParentChildren;
using Schoolera.Application.Parent.Queries.GetParentDashboard;
using Schoolera.Application.Parent.Queries.GetParentProfile;

namespace Schoolera.Api.Controllers;

[Route("api/parent")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentController(
    ISender mediator,
    ILogger<ParentController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<Result<ParentDashboardDto>>> GetDashboard(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetParentDashboardQuery(), cancellationToken));
    }

    [HttpGet("profile")]
    public async Task<ActionResult<Result<ParentProfileDto>>> GetProfile(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetParentProfileQuery(), cancellationToken));
    }

    [HttpPut("profile")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<ParentProfileDto>>> UpdateProfile(
        [FromBody] UpdateParentProfileRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new UpdateParentProfileCommand(body), cancellationToken));
    }

    [HttpGet("children")]
    public async Task<ActionResult<Result<IReadOnlyList<ChildProfileListItemDto>>>> GetChildren(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetParentChildrenQuery(), cancellationToken));
    }

    [HttpGet("children/{childId:guid}")]
    public async Task<ActionResult<Result<ChildProfileDetailDto>>> GetChild(
        Guid childId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetParentChildQuery(childId), cancellationToken));
    }

    [HttpPost("children")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<ChildProfileDetailDto>>> CreateChild(
        [FromBody] CreateChildProfileRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new CreateChildProfileCommand(body), cancellationToken));
    }

    [HttpPut("children/{childId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<ChildProfileDetailDto>>> UpdateChild(
        Guid childId,
        [FromBody] UpdateChildProfileRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateChildProfileCommand(childId, body),
            cancellationToken));
    }

    [HttpDelete("children/{childId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<bool>>> DeleteChild(
        Guid childId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new DeleteChildProfileCommand(childId), cancellationToken));
    }
}
