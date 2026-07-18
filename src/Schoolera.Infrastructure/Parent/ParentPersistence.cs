using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure.Identity;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Parent;

public sealed class ParentAccountService(
    UserManager<ApplicationUser> userManager,
    SchooleraDbContext dbContext) : IParentAccountService
{
    public async Task<ParentAccountSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        return user is null
            ? null
            : new ParentAccountSnapshot(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.PreferredLanguage);
    }

    public async Task UpdateBasicAsync(
        Guid userId,
        string firstName,
        string lastName,
        string phone,
        string preferredLanguage,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("Parent user was not found.");

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.PhoneNumber = phone.Trim();
        user.PreferredLanguage = preferredLanguage.Trim().ToLowerInvariant() switch
        {
            "en" => "en",
            _ => "ar",
        };
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}

public sealed class ParentProfileRepository(SchooleraDbContext dbContext) : IParentProfileRepository
{
    public Task<Domain.Entities.ParentProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.ParentProfiles
            .AsNoTracking()
            .Include(profile => profile.Country)
            .Include(profile => profile.Governorate)
            .Include(profile => profile.City)
            .Include(profile => profile.District)
            .FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public Task<Domain.Entities.ParentProfile?> GetByUserIdForUpdateAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.ParentProfiles
            .Include(profile => profile.Country)
            .Include(profile => profile.Governorate)
            .Include(profile => profile.City)
            .Include(profile => profile.District)
            .FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public async Task AddAsync(
        Domain.Entities.ParentProfile profile,
        CancellationToken cancellationToken = default) =>
        await dbContext.ParentProfiles.AddAsync(profile, cancellationToken);
}

public sealed class ChildProfileRepository(SchooleraDbContext dbContext) : IChildProfileRepository
{
    public async Task<IReadOnlyList<Domain.Entities.ChildProfile>> ListByParentUserIdAsync(
        Guid parentUserId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ChildProfiles
            .AsNoTracking()
            .Include(child => child.CurrentGrade)
            .Where(child => child.ParentUserId == parentUserId);

        if (!includeInactive)
        {
            query = query.Where(child => child.IsActive);
        }

        return await query
            .OrderBy(child => child.FullName)
            .ThenBy(child => child.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Domain.Entities.ChildProfile?> GetOwnedAsync(
        Guid parentUserId,
        Guid childId,
        CancellationToken cancellationToken = default) =>
        dbContext.ChildProfiles
            .AsNoTracking()
            .Include(child => child.CurrentGrade)
            .FirstOrDefaultAsync(
                child => child.Id == childId && child.ParentUserId == parentUserId,
                cancellationToken);

    public Task<Domain.Entities.ChildProfile?> GetOwnedForUpdateAsync(
        Guid parentUserId,
        Guid childId,
        CancellationToken cancellationToken = default) =>
        dbContext.ChildProfiles
            .Include(child => child.CurrentGrade)
            .FirstOrDefaultAsync(
                child => child.Id == childId && child.ParentUserId == parentUserId,
                cancellationToken);

    public Task<bool> IdentityHashExistsAsync(
        Guid parentUserId,
        string identityLookupHash,
        Guid? excludeChildId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ChildProfiles.AsNoTracking()
            .Where(child =>
                child.ParentUserId == parentUserId &&
                child.IdentityLookupHash == identityLookupHash);

        if (excludeChildId is { } id)
        {
            query = query.Where(child => child.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        Domain.Entities.ChildProfile child,
        CancellationToken cancellationToken = default) =>
        await dbContext.ChildProfiles.AddAsync(child, cancellationToken);

    public void Remove(Domain.Entities.ChildProfile child) =>
        dbContext.ChildProfiles.Remove(child);
}

public sealed class ChildDocumentRepository(SchooleraDbContext dbContext) : IChildDocumentRepository
{
    public async Task<IReadOnlyList<Domain.Entities.ChildDocument>> ListByChildAsync(
        Guid parentUserId,
        Guid childId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ChildDocuments
            .AsNoTracking()
            .Where(document =>
                document.ParentUserId == parentUserId &&
                document.ChildProfileId == childId)
            .OrderByDescending(document => document.CreatedAtUtc)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

    public Task<Domain.Entities.ChildDocument?> GetOwnedAsync(
        Guid parentUserId,
        Guid childId,
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        dbContext.ChildDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                document =>
                    document.Id == documentId &&
                    document.ChildProfileId == childId &&
                    document.ParentUserId == parentUserId,
                cancellationToken);

    public Task<Domain.Entities.ChildDocument?> GetOwnedForUpdateAsync(
        Guid parentUserId,
        Guid childId,
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        dbContext.ChildDocuments
            .FirstOrDefaultAsync(
                document =>
                    document.Id == documentId &&
                    document.ChildProfileId == childId &&
                    document.ParentUserId == parentUserId,
                cancellationToken);

    public async Task AddAsync(
        Domain.Entities.ChildDocument document,
        CancellationToken cancellationToken = default) =>
        await dbContext.ChildDocuments.AddAsync(document, cancellationToken);

    public void Remove(Domain.Entities.ChildDocument document) =>
        dbContext.ChildDocuments.Remove(document);

    public Task<bool> ExistsSourceOnApplicationAsync(
        Guid applicationId,
        Guid vaultDocumentId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAttachments
            .AsNoTracking()
            .AnyAsync(
                attachment =>
                    attachment.AdmissionApplicationId == applicationId &&
                    attachment.SourceVaultDocumentId == vaultDocumentId,
                cancellationToken);

    public Task<Domain.Entities.AdmissionApplicationAttachment?> GetSourceLinkedAttachmentAsync(
        Guid applicationId,
        Guid vaultDocumentId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                attachment =>
                    attachment.AdmissionApplicationId == applicationId &&
                    attachment.SourceVaultDocumentId == vaultDocumentId,
                cancellationToken);
}
