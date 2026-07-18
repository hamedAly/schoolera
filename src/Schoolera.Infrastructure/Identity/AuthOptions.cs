namespace Schoolera.Infrastructure.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public CookieOptionsSection Cookie { get; set; } = new();

    public VerificationOptionsSection Verification { get; set; } = new();

    public PasswordResetOptionsSection PasswordReset { get; set; } = new();

    public SeedUsersOptionsSection SeedUsers { get; set; } = new();
}

public sealed class CookieOptionsSection
{
    public string CookieName { get; set; } = "Schoolera.Auth";

    public int ExpireMinutes { get; set; } = 60 * 24 * 14;

    public bool SlidingExpiration { get; set; } = true;
}

public sealed class VerificationOptionsSection
{
    public int CodeLength { get; set; } = 6;

    public int ExpirationMinutes { get; set; } = 15;

    public int ResendCooldownSeconds { get; set; } = 60;
}

public sealed class PasswordResetOptionsSection
{
    public int TokenExpirationMinutes { get; set; } = 60;
}

public sealed class SeedUsersOptionsSection
{
    public string DefaultPassword { get; set; } = string.Empty;

    public SeedUserEntry Parent { get; set; } = new();

    public SeedUserEntry SchoolOwner { get; set; } = new();

    public SeedUserEntry SchoolAdmin { get; set; } = new();

    public SeedUserEntry AdmissionOfficer { get; set; } = new();

    public SeedUserEntry FinanceOfficer { get; set; } = new();

    public SeedUserEntry ContentModerator { get; set; } = new();

    public SeedUserEntry PlatformAdmin { get; set; } = new();

    public SeedUserEntry SupportAgent { get; set; } = new();
}

public sealed class SeedUserEntry
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
