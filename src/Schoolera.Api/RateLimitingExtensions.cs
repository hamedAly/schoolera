using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Api;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddSchooleraRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.HttpContext.Response.HasStarted)
                {
                    return;
                }

                var localizer = context.HttpContext.RequestServices
                    .GetRequiredService<IStringLocalizer<AuthMessages>>();

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var result = Result<object>.Failure(
                    [localizer["RateLimited"].Value],
                    [AuthErrorCodes.RateLimited]);

                await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
            };

            options.AddPolicy("auth-register", httpContext =>
                CreateFixedWindowLimiter(httpContext, permitLimit: 5, windowMinutes: 15));

            // Development hosts (including WebApplicationFactory) need a higher login budget for
            // integration suites; production keeps the stricter window.
            options.AddPolicy("auth-login", httpContext =>
            {
                var env = httpContext.RequestServices.GetService<IHostEnvironment>();
                var limit = env?.IsDevelopment() == true ? 1000 : 10;
                return CreateFixedWindowLimiter(httpContext, permitLimit: limit, windowMinutes: 15);
            });

            options.AddPolicy("auth-verify", httpContext =>
                CreateFixedWindowLimiter(httpContext, permitLimit: 10, windowMinutes: 15));

            options.AddPolicy("auth-resend-verification", httpContext =>
                CreateFixedWindowLimiter(httpContext, permitLimit: 5, windowMinutes: 15));

            options.AddPolicy("auth-forgot-password", httpContext =>
                CreateFixedWindowLimiter(httpContext, permitLimit: 5, windowMinutes: 15));

            options.AddPolicy("auth-reset-password", httpContext =>
                CreateFixedWindowLimiter(httpContext, permitLimit: 5, windowMinutes: 15));

            options.AddPolicy("auth-logout", httpContext =>
                CreateFixedWindowLimiter(httpContext, permitLimit: 30, windowMinutes: 15));

            options.AddPolicy("schools-contact-lead", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var slug = httpContext.Request.RouteValues.TryGetValue("slug", out var value)
                    ? value?.ToString() ?? string.Empty
                    : string.Empty;
                var partitionKey = $"{ip}:{slug}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });

            options.AddPolicy("schools-map-pins", httpContext =>
            {
                var env = httpContext.RequestServices.GetService<IHostEnvironment>();
                var limit = env?.IsDevelopment() == true ? 1000 : 60;
                return CreateFixedWindowLimiter(httpContext, permitLimit: limit, windowMinutes: 1);
            });

            options.AddPolicy("contact-submit", httpContext =>
            {
                var env = httpContext.RequestServices.GetService<IHostEnvironment>();
                var limit = env?.IsDevelopment() == true ? 1000 : 5;
                return CreateFixedWindowLimiter(httpContext, permitLimit: limit, windowMinutes: 1);
            });

            options.AddPolicy("courier-health-check", httpContext =>
                CreateFixedWindowLimiter(httpContext, permitLimit: 10, windowMinutes: 1));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowLimiter(
        HttpContext httpContext,
        int permitLimit,
        int windowMinutes)
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(windowMinutes),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }
}
