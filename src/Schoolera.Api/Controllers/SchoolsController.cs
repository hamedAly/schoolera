using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Commands.CreateSchool;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Application.Schools.Queries.GetSchools;

namespace Schoolera.Api.Controllers;

[Route("api/schools")]
public sealed class SchoolsController(
    ISender mediator,
    ILogger<SchoolsController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public async Task<Result<IReadOnlyCollection<SchoolDto>>> GetSchools(
        CancellationToken cancellationToken)
    {
        var schools = await Mediator.Send(new GetSchoolsQuery(), cancellationToken);
        return Success(schools);
    }

    [HttpPost]
    public async Task<Result<SchoolDto>> CreateSchool(
        CreateSchoolCommand command,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating school {SchoolName}.", command.Name);
        var school = await Mediator.Send(command, cancellationToken);
        return Success(school);
    }
}