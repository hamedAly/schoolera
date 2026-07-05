using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase(ISender mediator, ILogger logger) : ControllerBase
{
    protected ISender Mediator { get; } = mediator;

    protected ILogger Logger { get; } = logger;

    protected static Result<T> Success<T>(T data)
    {
        return Result<T>.Success(data);
    }

    protected static Result<T> Failure<T>(string error)
    {
        return Result<T>.Failure(new[] { error });
    }

    protected static Result<T> Failure<T>(IEnumerable<string> errors)
    {
        return Result<T>.Failure(errors);
    }
}