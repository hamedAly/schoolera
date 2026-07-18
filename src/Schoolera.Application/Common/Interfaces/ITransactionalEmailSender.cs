namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// Narrow transactional email sender for auth verification (and similar) messages.
/// Controllers must not compose HTML; implementations own templates.
/// </summary>
public interface ITransactionalEmailSender
{
    string Mode { get; }

    Task SendVerificationCodeAsync(
        VerificationEmailMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record VerificationEmailMessage(
    string RecipientEmail,
    string? RecipientDisplayName,
    string VerificationCode,
    int ExpirationMinutes,
    string Language);
