using FluentValidation;
using Microsoft.AspNetCore.Http;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToArray();

            await WriteFailureAsync(context, StatusCodes.Status400BadRequest, errors);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteFailureAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ["An unexpected error occurred."]);
        }
    }

    private static async Task WriteFailureAsync(
        HttpContext context,
        int statusCode,
        IReadOnlyCollection<string> errors)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var result = Result<object>.Failure(errors);

        await context.Response.WriteAsJsonAsync(result);
    }
}