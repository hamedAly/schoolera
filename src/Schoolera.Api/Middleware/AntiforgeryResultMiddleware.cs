using Microsoft.AspNetCore.Antiforgery;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Middleware;

public sealed class AntiforgeryResultMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AntiforgeryValidationException)
        {
            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var result = Result<object>.Failure(
                ["Invalid or missing antiforgery token."],
                [ErrorCodes.Validation]);

            await context.Response.WriteAsJsonAsync(result);
        }
    }
}
