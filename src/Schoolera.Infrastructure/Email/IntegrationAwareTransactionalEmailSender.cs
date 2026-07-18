using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Notifications;

namespace Schoolera.Infrastructure.Email;

/// <summary>
/// Transactional verification email sender that resolves Email provider settings from
/// <see cref="IPlatformIntegrationConfigurationAccessor"/> (database), not appsettings credentials.
/// </summary>
public sealed class IntegrationAwareTransactionalEmailSender(
    IPlatformIntegrationConfigurationAccessor integrationAccessor,
    IHostEnvironment hostEnvironment,
    ILogger<IntegrationAwareTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    public string Mode => "IntegrationAware";

    public async Task SendVerificationCodeAsync(
        VerificationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var integration = await integrationAccessor.GetActiveDefaultAsync(
            IntegrationType.Email,
            cancellationToken);

        if (integration is null)
        {
            if (hostEnvironment.IsDevelopment())
            {
                logger.LogWarning(
                    "No active default Email integration configured. Verification code was stored; email delivery skipped in Development for {MaskedRecipient}.",
                    NotificationRecipientMasking.MaskEmail(message.RecipientEmail));
                return;
            }

            throw new InvalidOperationException(
                "No active default Email integration is configured for transactional email.");
        }

        if (IntegrationProviderCodes.IsSimulated(integration.ProviderCode))
        {
            // Do not log OTP. Verification code remains in VerificationCodes table.
            logger.LogInformation(
                "Simulated/Development email integration accepted verification message for {MaskedRecipient}.",
                NotificationRecipientMasking.MaskEmail(message.RecipientEmail));
            return;
        }

        if (!string.Equals(integration.ProviderCode, IntegrationProviderCodes.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            if (hostEnvironment.IsDevelopment())
            {
                logger.LogWarning(
                    "Unsupported Email provider {ProviderCode}; verification email skipped in Development for {MaskedRecipient}.",
                    integration.ProviderCode,
                    NotificationRecipientMasking.MaskEmail(message.RecipientEmail));
                return;
            }

            throw new InvalidOperationException(
                $"Email provider '{integration.ProviderCode}' is not supported for transactional email.");
        }

        EmailIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<EmailIntegrationSettings>(integration.SettingsJson);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Email integration SettingsJson is invalid.", exception);
        }

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.SenderEmail) ||
            settings.Port is null or < 1 or > 65535)
        {
            if (hostEnvironment.IsDevelopment())
            {
                logger.LogWarning(
                    "SMTP Email integration is missing required settings; verification email skipped in Development for {MaskedRecipient}.",
                    NotificationRecipientMasking.MaskEmail(message.RecipientEmail));
                return;
            }

            throw new InvalidOperationException(
                "SMTP Email integration is missing Host, Port, or SenderEmail in SettingsJson.");
        }

        using var client = new SmtpClient(settings.Host, settings.Port.Value)
        {
            EnableSsl = settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = Math.Clamp(settings.RequestTimeoutSeconds, 1, 120) * 1000,
        };

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        }

        using var mail = BuildMessage(settings, message);
        await client.SendMailAsync(mail, cancellationToken);

        logger.LogInformation(
            "Verification email accepted by SMTP for {MaskedRecipient}.",
            NotificationRecipientMasking.MaskEmail(message.RecipientEmail));
    }

    private static MailMessage BuildMessage(EmailIntegrationSettings settings, VerificationEmailMessage message)
    {
        var isArabic = message.Language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var displayName = string.IsNullOrWhiteSpace(message.RecipientDisplayName)
            ? message.RecipientEmail
            : message.RecipientDisplayName.Trim();

        var subject = isArabic
            ? "رمز تأكيد البريد الإلكتروني في Schoolera"
            : "Verify your Schoolera email address";

        var body = isArabic
            ? $"""
               مرحبًا {displayName},

               تم إنشاء حساب على Schoolera باستخدام هذا البريد الإلكتروني.

               رمز التحقق الخاص بك هو: {message.VerificationCode}

               صلاحية الرمز {message.ExpirationMinutes} دقيقة.

               إذا لم تقم بإنشاء هذا الحساب، يمكنك تجاهل هذه الرسالة بأمان.

               Schoolera
               """
            : $"""
               Hello {displayName},

               A Schoolera account was created with this email address.

               Your verification code is: {message.VerificationCode}

               This code expires in {message.ExpirationMinutes} minutes.

               If you did not create this account, you can safely ignore this email.

               Schoolera
               """;

        var mail = new MailMessage
        {
            From = new MailAddress(settings.SenderEmail!, settings.SenderName ?? "Schoolera"),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
            To = { new MailAddress(message.RecipientEmail, displayName) },
        };

        if (!string.IsNullOrWhiteSpace(settings.ReplyToEmail))
        {
            mail.ReplyToList.Add(new MailAddress(settings.ReplyToEmail));
        }

        return mail;
    }
}
