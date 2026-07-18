using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using Schoolera.Api.Resources;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Middleware;

/// <summary>
/// Converts cookie-auth 401/403 responses into JSON <see cref="Result{T}"/> payloads.
/// </summary>
public sealed class AuthorizationResultMiddleware(
    RequestDelegate next,
    IStringLocalizer<ApiMessages> localizer)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status401Unauthorized,
                localizer["Unauthorized"].Value,
                AuthErrorCodes.Unauthorized);
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status403Forbidden,
                localizer["Forbidden"].Value,
                AuthErrorCodes.Forbidden);
        }
    }

    private static async Task WriteFailureAsync(
        HttpContext context,
        int statusCode,
        string message,
        string errorCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var result = Result<object>.Failure([message], [errorCode]);
        await context.Response.WriteAsJsonAsync(result);
    }
}
