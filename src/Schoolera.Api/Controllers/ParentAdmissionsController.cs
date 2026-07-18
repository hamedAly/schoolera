using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Admissions.Queries.CheckParentAgeEligibility;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/parent/admissions")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentAdmissionsController(
    ISender mediator,
    ILogger<ParentAdmissionsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpPost("age-eligibility-check")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<AgeEligibilityResultDto>>> CheckAgeEligibility(
        [FromBody] ParentAgeEligibilityCheckRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CheckParentAgeEligibilityQuery(body),
            cancellationToken));
    }
}
