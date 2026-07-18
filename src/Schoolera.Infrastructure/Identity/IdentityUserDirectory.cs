using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Identity;

/// <summary>Reads basic identity user info for admin projections.</summary>
public sealed class IdentityUserDirectory(SchooleraDbContext dbContext) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, UserSummary>> GetUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, UserSummary>();
        }

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
            })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            user => user.Id,
            user => new UserSummary(
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email ?? string.Empty,
                user.PhoneNumber));
    }

    public async Task<UserSummary?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(entry => entry.NormalizedEmail == normalized)
            .Select(entry => new
            {
                entry.Id,
                entry.FirstName,
                entry.LastName,
                entry.Email,
                entry.PhoneNumber,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? null
            : new UserSummary(
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email ?? string.Empty,
                user.PhoneNumber);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select role.Name!)
            .ToListAsync(cancellationToken);
    }
}
