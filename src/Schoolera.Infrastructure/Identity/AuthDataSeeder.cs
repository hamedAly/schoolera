using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Identity;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure;

public sealed class AuthDataSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    SchooleraDbContext dbContext,
    IOptions<AuthOptions> authOptions,
    ILogger<AuthDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var role in SchooleraRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
            });

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed role '{role}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }

            logger.LogInformation("Seeded role {Role}.", role);
        }
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var options = authOptions.Value.SeedUsers;
        var password = options.DefaultPassword;

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Auth seed users skipped because Auth:SeedUsers:DefaultPassword is empty.");
            return;
        }

        await SeedUserAsync(options.Parent, SchooleraRoles.Parent, password, AccountStatus.Active, cancellationToken);
        await SeedUserAsync(options.SchoolOwner, SchooleraRoles.SchoolOwner, password, AccountStatus.Active, cancellationToken);
        await SeedUserAsync(options.SchoolAdmin, SchooleraRoles.SchoolAdmin, password, AccountStatus.Active, cancellationToken);
        await SeedUserAsync(options.AdmissionOfficer, SchooleraRoles.AdmissionOfficer, password, AccountStatus.Active, cancellationToken);
        await SeedUserAsync(options.FinanceOfficer, SchooleraRoles.FinanceOfficer, password, AccountStatus.Active, cancellationToken);
        await SeedUserAsync(options.ContentModerator, SchooleraRoles.ContentModerator, password, AccountStatus.Active, cancellationToken);
        await SeedUserAsync(options.PlatformAdmin, SchooleraRoles.PlatformAdmin, password, AccountStatus.Active, cancellationToken);
        await SeedUserAsync(options.SupportAgent, SchooleraRoles.SupportAgent, password, AccountStatus.Active, cancellationToken);
    }

    private async Task SeedUserAsync(
        SeedUserEntry entry,
        string role,
        string password,
        AccountStatus status,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entry.Email))
        {
            return;
        }

        var normalizedEmail = userManager.NormalizeEmail(entry.Email);
        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

        if (exists)
        {
            logger.LogInformation("Seed auth user skipped (already exists by email): {Email}.", entry.Email);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = entry.Email,
            Email = entry.Email,
            PhoneNumber = entry.PhoneNumber,
            FirstName = entry.FirstName,
            LastName = entry.LastName,
            PreferredLanguage = "ar",
            AccountStatus = status,
            EmailConfirmed = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user '{entry.Email}': {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to assign role '{role}' to '{entry.Email}': {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
        }

        logger.LogInformation("Seeded auth user {Email} with role {Role}.", entry.Email, role);
    }
}
