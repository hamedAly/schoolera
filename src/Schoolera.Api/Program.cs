using Microsoft.OpenApi;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Schoolera.Api;
using Schoolera.Api.Middleware;
using Schoolera.Api.Options;
using Schoolera.Api.Resources;
using Schoolera.Api.Services;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application;
using Schoolera.Application.Common.Models;
using Schoolera.Infrastructure;
using Schoolera.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// ControllersWithViews registers ValidateAntiForgeryTokenAuthorizationFilter required by
// [ValidateAntiForgeryToken] on cookie-authenticated API endpoints (auth, portal, admin).
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Schoolera API",
        Version = "v1"
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddSchooleraRateLimiting();
builder.Services.Configure<NswagOptions>(builder.Configuration.GetSection(NswagOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<HttpsRedirectionSettings>(
    builder.Configuration.GetSection(HttpsRedirectionSettings.SectionName));
builder.Services.AddHttpClient(nameof(NSwagClientService));
builder.Services.AddSingleton<NSwagClientService>();

builder.Services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .PostConfigure<IStringLocalizer<ApiMessages>>((options, localizer) =>
    {
        options.Events.OnRedirectToLogin = async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var result = Result<object>.Failure(
                    [localizer["Unauthorized"].Value],
                    [AuthErrorCodes.Unauthorized]);
                await context.Response.WriteAsJsonAsync(result);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        };

        options.Events.OnRedirectToAccessDenied = async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var result = Result<object>.Failure(
                    [localizer["Forbidden"].Value],
                    [AuthErrorCodes.Forbidden]);
                await context.Response.WriteAsJsonAsync(result);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        };
    });

builder.Services.AddAntiforgery(options =>
{
    // Server antiforgery cookie stays HttpOnly (default name). The readable
    // XSRF-TOKEN cookie is written separately with the request token in AuthController.Me
    // for Angular HttpClientXsrfModule (header X-XSRF-TOKEN).
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Development CORS for Angular ng serve. Origins come from Cors:AllowedOrigins — never hardcoded.
// Prefer the Angular proxy (relative /api) so the browser stays same-origin; CORS covers direct
// cross-origin calls and credentials when a configured origin is used.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()?
            .AllowedOrigins?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];

        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Controlled by appsettings Database:ApplyMigrations / Database:SeedData.
await app.Services.InitializeDatabaseAsync();

var supportedCultures = new[] { "ar-EG", "en-US", "ar", "en" };
app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("ar-EG")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
    options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider());
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger is enabled in Development by default, or when Swagger:Enabled is set.
var swaggerEnabled = app.Configuration.GetValue<bool?>("Swagger:Enabled")
    ?? app.Environment.IsDevelopment();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Schoolera API v1");
        options.RoutePrefix = "swagger";
    });
}

// Development: keep HTTP on 5085 so the Angular proxy is not bounced to HTTPS:7076 (307 + CORS).
// Production: HTTPS redirection remains enabled via HttpsRedirection:Enabled (default true).
var httpsRedirectionEnabled = app.Configuration.GetValue(
    $"{HttpsRedirectionSettings.SectionName}:Enabled",
    !app.Environment.IsDevelopment());

if (httpsRedirectionEnabled)
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("AngularDev");
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<AuthorizationResultMiddleware>();
app.UseMiddleware<AntiforgeryCookieMiddleware>();
app.UseMiddleware<AntiforgeryResultMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

var fileStorageOptions = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
Directory.CreateDirectory(fileStorageOptions.StorageRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fileStorageOptions.StorageRoot),
    RequestPath = fileStorageOptions.PublicRequestPath,
});

// API routes must be registered before Angular fallback.
app.MapControllers();

// Unmatched /api, /swagger, and /uploads routes must not fall through to Angular index.html.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallback("/swagger/{**path}", () => Results.NotFound());
app.MapFallback($"{fileStorageOptions.PublicRequestPath.TrimEnd('/')}/{{**path}}", () => Results.NotFound());

// Must always remain the final endpoint mapping.
app.MapFallbackToFile("index.html");

if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Nswag:Enabled", false))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var nswagClientService = scope.ServiceProvider.GetRequiredService<NSwagClientService>();
                await nswagClientService.GenerateAsync();
            }
            catch (Exception exception)
            {
                var logger = app.Services
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("NSwagClientGeneration");

                logger.LogError(exception, "NSwag Angular client generation failed.");
            }
        });
    });
}

app.Run();

public partial class Program;
