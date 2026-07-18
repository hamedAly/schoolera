using Schoolera.Application.Schools.Dtos;

namespace Schoolera.Application.Favorites.Dtos;

public sealed record FavoriteSchoolUnavailableDto(
    Guid Id,
    Guid SchoolId,
    string? Slug,
    string DisplayName,
    bool IsAvailable = false,
    bool IsAdmissionOpen = false);

/// <summary>
/// Parent favorites list item. When <see cref="IsAvailable"/> is true, <see cref="School"/> is populated
/// with a public card. Otherwise <see cref="Unavailable"/> holds a safe summary.
/// </summary>
public sealed record FavoriteSchoolListItemDto(
    Guid FavoriteId,
    Guid SchoolId,
    bool IsAvailable,
    PublicSchoolListItemDto? School,
    FavoriteSchoolUnavailableDto? Unavailable);
