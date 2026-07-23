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
        catch (OperationCanceledException) when (IsClientAborted(context))
        {
            // Client disconnected (tab switch, navigation, replaced request). Not an application fault.
            logger.LogInformation(
                "Client canceled request. CorrelationId={CorrelationId} Method={Method} Path={Path}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path.Value);
        }
        catch (Exception exception) when (IsClientAborted(context) && IsCancellationException(exception))
        {
            logger.LogDebug(
                exception,
                "Client canceled request (wrapped). CorrelationId={CorrelationId} Method={Method} Path={Path}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path.Value);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await WriteFailureAsync(
                context,
                StatusCodes.Status500InternalServerError,
                [localizer["UnexpectedError"].Value],
                [ErrorCodes.Unexpected]);
        }
    }

    private static bool IsClientAborted(HttpContext context) =>
        context.RequestAborted.IsCancellationRequested;

    private static bool IsCancellationException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException)
            {
                return true;
            }
        }

        return false;
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
        // Never write after the client has disconnected or a response has already started.
        if (context.RequestAborted.IsCancellationRequested || context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var result = Result<object>.Failure(errors, errorCodes);

        await context.Response.WriteAsJsonAsync(result);
    }
}
