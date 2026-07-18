using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure;

public static class IdentityDependencyInjection
{
    public static IServiceCollection AddSchooleraIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<Persistence.SchooleraDbContext>()
            .AddDefaultTokenProviders();

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = authOptions.Cookie.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.SlidingExpiration = authOptions.Cookie.SlidingExpiration;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(authOptions.Cookie.ExpireMinutes);
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(SchooleraPolicies.ParentOnly, policy =>
                policy.RequireRole(SchooleraRoles.Parent))
            .AddPolicy(SchooleraPolicies.SchoolPortal, policy =>
                policy.RequireRole(
                    SchooleraRoles.SchoolOwner,
                    SchooleraRoles.SchoolAdmin,
                    SchooleraRoles.AdmissionOfficer,
                    SchooleraRoles.FinanceOfficer,
                    SchooleraRoles.ContentModerator))
            .AddPolicy(SchooleraPolicies.SchoolOwnerOnly, policy =>
                policy.RequireRole(SchooleraRoles.SchoolOwner))
            .AddPolicy(SchooleraPolicies.PlatformAdminOnly, policy =>
                policy.RequireRole(SchooleraRoles.PlatformAdmin))
            .AddPolicy(SchooleraPolicies.SupportOrAdmin, policy =>
                policy.RequireRole(SchooleraRoles.SupportAgent, SchooleraRoles.PlatformAdmin));

        services.AddScoped<IAuthAccountService, IdentityAuthAccountService>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<AuthDataSeeder>();

        return services;
    }

    public static IServiceCollection AddSchooleraEmail(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ = environment;

        services.AddSingleton<IValidateOptions<Email.EmailOptions>, Email.EmailOptionsStartupValidator>();
        services
            .AddOptions<Email.EmailOptions>()
            .Bind(configuration.GetSection(Email.EmailOptions.SectionName))
            .ValidateOnStart();

        // Provider credentials come from PlatformIntegrationConfiguration (DB).
        // Legacy Email:Mode / Smtp appsettings remain for backward compatibility only.
        services.AddScoped<ITransactionalEmailSender, Email.IntegrationAwareTransactionalEmailSender>();

        return services;
    }
}
