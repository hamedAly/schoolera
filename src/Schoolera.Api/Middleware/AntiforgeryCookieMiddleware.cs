using Microsoft.AspNetCore.Antiforgery;

namespace Schoolera.Api.Middleware;

/// <summary>
/// Ensures the SPA-readable <c>XSRF-TOKEN</c> cookie carries the antiforgery request token
/// after the server cookie token is stored. Angular sends that value as <c>X-XSRF-TOKEN</c>.
/// </summary>
public sealed class AntiforgeryCookieMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (context.Request.Path.StartsWithSegments("/api/auth/me", StringComparison.OrdinalIgnoreCase))
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            if (!string.IsNullOrEmpty(tokens.RequestToken))
            {
                context.Response.Cookies.Append(
                    "XSRF-TOKEN",
                    tokens.RequestToken,
                    new CookieOptions
                    {
                        HttpOnly = false,
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = context.Request.IsHttps,
                        Path = "/",
                    });
            }
        }

        await next(context);
    }
}
