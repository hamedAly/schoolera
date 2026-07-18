using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface IParentProfileRepository
{
    Task<ParentProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ParentProfile?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(ParentProfile profile, CancellationToken cancellationToken = default);
}

public interface IChildProfileRepository
{
    Task<IReadOnlyList<ChildProfile>> ListByParentUserIdAsync(
        Guid parentUserId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<ChildProfile?> GetOwnedAsync(
        Guid parentUserId,
        Guid childId,
        CancellationToken cancellationToken = default);

    Task<ChildProfile?> GetOwnedForUpdateAsync(
        Guid parentUserId,
        Guid childId,
        CancellationToken cancellationToken = default);

    Task<bool> IdentityHashExistsAsync(
        Guid parentUserId,
        string identityLookupHash,
        Guid? excludeChildId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ChildProfile child, CancellationToken cancellationToken = default);

    void Remove(ChildProfile child);
}

public interface IParentAccountService
{
    Task<ParentAccountSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpdateBasicAsync(
        Guid userId,
        string firstName,
        string lastName,
        string phone,
        string preferredLanguage,
        CancellationToken cancellationToken = default);
}

public sealed record ParentAccountSnapshot(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string PreferredLanguage);

public interface IChildIdentityProtector
{
    string Normalize(string identityValue);

    bool IsValidFormat(ChildIdentityType identityType, string normalizedValue);

    string ComputeLookupHash(string normalizedValue);

    string Protect(string normalizedValue);

    string ExtractLastFour(string normalizedValue);

    string Mask(string identityLastFour);
}

public static class ChildIdentityMasking
{
    public static string MaskLastFour(string lastFour) =>
        string.IsNullOrWhiteSpace(lastFour) ? "************" : $"************{lastFour.Trim()}";

    /// <summary>
    /// Blank or already-masked identity input means keep the stored protected value.
    /// </summary>
    public static bool IsBlankOrMasked(string? identityValue, string? existingLastFour)
    {
        if (string.IsNullOrWhiteSpace(identityValue))
        {
            return true;
        }

        var trimmed = identityValue.Trim();
        if (trimmed.Contains('*', StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(existingLastFour) &&
            string.Equals(trimmed, MaskLastFour(existingLastFour), StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
