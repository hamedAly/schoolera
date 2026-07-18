namespace Schoolera.Application.Common.Interfaces;

/// <summary>Read-only lookup of basic identity user info for admin projections.</summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, UserSummary>> GetUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<UserSummary?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record UserSummary(
    Guid Id,
    string DisplayName,
    string Email,
    string? PhoneNumber = null);
