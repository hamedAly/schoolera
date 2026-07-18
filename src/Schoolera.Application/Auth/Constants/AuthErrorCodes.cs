namespace Schoolera.Application.Auth.Constants;

/// <summary>
/// Stable authentication error codes. Frontends branch on these, never on localized text.
/// </summary>
public static class AuthErrorCodes
{
    public const string InvalidCredentials = "auth.invalidCredentials";
    public const string AccountSuspended = "auth.accountSuspended";
    public const string AccountNotVerified = "auth.accountNotVerified";

    /// <summary>Preferred code for duplicate email / username collisions.</summary>
    public const string EmailAlreadyExists = "auth.emailAlreadyExists";

    /// <summary>Alias of <see cref="EmailAlreadyExists"/> for existing call sites.</summary>
    public const string DuplicateEmail = EmailAlreadyExists;

    public const string DuplicatePhone = "auth.duplicatePhone";
    public const string InvalidEmail = "auth.invalidEmail";
    public const string InvalidVerificationCode = "auth.invalidVerificationCode";
    public const string ExpiredVerificationCode = "auth.expiredVerificationCode";
    public const string CodeAlreadyUsed = "auth.codeAlreadyUsed";
    public const string EmailAlreadyVerified = "auth.emailAlreadyVerified";
    public const string ResendTooSoon = "auth.resendTooSoon";
    public const string DeliveryFailed = "auth.deliveryFailed";
    public const string RateLimited = "auth.rateLimited";
    public const string InvalidResetToken = "auth.invalidResetToken";
    public const string Unauthorized = "auth.unauthorized";
    public const string Forbidden = "auth.forbidden";

    public const string PasswordTooShort = "auth.passwordTooShort";
    public const string PasswordRequiresNonAlphanumeric = "auth.passwordRequiresNonAlphanumeric";
    public const string PasswordRequiresLowercase = "auth.passwordRequiresLowercase";
    public const string PasswordRequiresUppercase = "auth.passwordRequiresUppercase";
    public const string PasswordRequiresDigit = "auth.passwordRequiresDigit";
    public const string PasswordRequiresUniqueChars = "auth.passwordRequiresUniqueChars";
}
