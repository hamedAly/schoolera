using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Domain.Entities;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure.Persistence;

/// <summary>Idempotent demo favorites for the seeded parent account.</summary>
public sealed class FavoritesSeeder(
    SchooleraDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ILogger<FavoritesSeeder> logger)
{
    private const string ParentEmail = "parent@schoolera.local";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running favorites seed...");

        var parent = await userManager.FindByEmailAsync(ParentEmail);
        if (parent is null)
        {
            logger.LogInformation("Favorites seed skipped; parent {Email} not found.", ParentEmail);
            return;
        }

        var schools = await dbContext.Schools.AsNoTracking()
            .OrderBy(school => school.CreatedAtUtc)
            .Take(3)
            .Select(school => new { school.Id, school.Status })
            .ToListAsync(cancellationToken);

        if (schools.Count == 0)
        {
            logger.LogInformation("Favorites seed skipped; no schools found.");
            return;
        }

        var added = 0;
        foreach (var school in schools)
        {
            var exists = await dbContext.FavoriteSchools.AnyAsync(
                favorite => favorite.ParentUserId == parent.Id && favorite.SchoolId == school.Id,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.FavoriteSchools.Add(new FavoriteSchool(parent.Id, school.Id));
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Favorites seed completed. Added {AddedCount} favorites (including status samples).",
            added);
    }
}
