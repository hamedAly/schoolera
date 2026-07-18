namespace Schoolera.Application.Auth.Dtos;

public sealed record CurrentUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    string? PhoneNumber,
    IReadOnlyCollection<string> Roles,
    string AccountStatus,
    string PreferredLanguage,
    string PostLoginDestination);
