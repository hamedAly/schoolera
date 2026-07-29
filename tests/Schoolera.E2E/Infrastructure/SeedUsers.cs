namespace Schoolera.E2E.Infrastructure;

public enum SeedRole
{
    Parent,
    SchoolOwner,
    SchoolAdmin,
    PlatformAdmin,
    SupportAgent,
}

public static class SeedUsers
{
    public static string Email(SeedRole role) => role switch
    {
        SeedRole.Parent => "parent@schoolera.local",
        SeedRole.SchoolOwner => "schoolowner@schoolera.local",
        SeedRole.SchoolAdmin => "schooladmin@schoolera.local",
        SeedRole.PlatformAdmin => "admin@schoolera.local",
        SeedRole.SupportAgent => "support@schoolera.local",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static string ExpectedPathPrefix(SeedRole role) => role switch
    {
        SeedRole.Parent => "/parent",
        SeedRole.SchoolOwner => "/school",
        SeedRole.SchoolAdmin => "/school",
        SeedRole.PlatformAdmin => "/admin",
        SeedRole.SupportAgent => "/support",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static string StorageStatePath(SeedRole role) =>
        Path.Combine(E2EConfig.AuthDirectory, $"{role.ToString().ToLowerInvariant()}.json");

    public static IReadOnlyList<SeedRole> All { get; } =
    [
        SeedRole.Parent,
        SeedRole.SchoolOwner,
        SeedRole.SchoolAdmin,
        SeedRole.PlatformAdmin,
        SeedRole.SupportAgent,
    ];
}
