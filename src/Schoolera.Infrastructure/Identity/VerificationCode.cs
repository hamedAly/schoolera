namespace Schoolera.Infrastructure.Identity;

public sealed class VerificationCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Purpose { get; set; } = "registration";

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public bool IsConsumed { get; set; }

    public ApplicationUser? User { get; set; }
}
