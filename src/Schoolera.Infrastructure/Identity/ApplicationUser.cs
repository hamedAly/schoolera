using Microsoft.AspNetCore.Identity;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PreferredLanguage { get; set; } = "ar";

    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingVerification;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
