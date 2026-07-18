using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Schoolera.Api.Resources;
using Schoolera.Application.Common.Exceptions;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IStringLocalizer<ApiMessages> localizer)
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

            var codes = exception.Errors
                .Select(ResolveValidationErrorCode)
                .ToArray();

            await WriteFailureAsync(context, StatusCodes.Status400BadRequest, errors, codes);
        }
        catch (ConcurrencyConflictException exception)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status409Conflict,
                [localizer["ConcurrentUpdate"].Value],
                [exception.ErrorCode]);
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
                [localizer["UnexpectedError"].Value],
                [ErrorCodes.Unexpected]);
        }
    }

    private static string ResolveValidationErrorCode(FluentValidation.Results.ValidationFailure failure)
    {
        if (!string.IsNullOrWhiteSpace(failure.ErrorCode) &&
            failure.ErrorCode.Contains('.', StringComparison.Ordinal))
        {
            return failure.ErrorCode;
        }

        return ErrorCodes.Validation;
    }

    private static async Task WriteFailureAsync(
        HttpContext context,
        int statusCode,
        IReadOnlyCollection<string> errors,
        IReadOnlyCollection<string> errorCodes)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var result = Result<object>.Failure(errors, errorCodes);

        await context.Response.WriteAsJsonAsync(result);
    }
}
