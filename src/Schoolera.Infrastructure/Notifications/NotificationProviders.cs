using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Notifications;

public sealed class InAppNotificationProvider : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task<NotificationSendResult> SendAsync(
        NotificationOutboxMessage message,
        ResolvedIntegrationConfiguration? integration,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new NotificationSendResult(
            Succeeded: true,
            IsRetryable: false,
            Skipped: false,
            ProviderMessageId: message.Id.ToString("N"),
            SafeFailureCode: null));
}

public sealed class SimulatedEmailNotificationProvider(
    IUserDirectory userDirectory,
    ILogger<SimulatedEmailNotificationProvider> logger) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(
        NotificationOutboxMessage message,
        ResolvedIntegrationConfiguration? integration,
        CancellationToken cancellationToken = default)
    {
        var users = await userDirectory.GetUsersAsync([message.RecipientUserId], cancellationToken);
        if (!users.TryGetValue(message.RecipientUserId, out var user) ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return new NotificationSendResult(false, false, false, null, "notifications.recipientEmailMissing");
        }

        logger.LogInformation(
            "Simulated email notification {MessageId} accepted for {MaskedRecipient}.",
            message.Id,
            NotificationRecipientMasking.MaskEmail(user.Email));

        return new NotificationSendResult(true, false, false, $"sim-email-{message.Id:N}", null);
    }
}

public sealed class SimulatedSmsNotificationProvider(
    IUserDirectory userDirectory,
    ILogger<SimulatedSmsNotificationProvider> logger) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task<NotificationSendResult> SendAsync(
        NotificationOutboxMessage message,
        ResolvedIntegrationConfiguration? integration,
        CancellationToken cancellationToken = default)
    {
        var users = await userDirectory.GetUsersAsync([message.RecipientUserId], cancellationToken);
        if (!users.TryGetValue(message.RecipientUserId, out var user) ||
            string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return new NotificationSendResult(false, false, false, null, "notifications.recipientPhoneMissing");
        }

        logger.LogInformation(
            "Simulated SMS notification {MessageId} accepted for {MaskedRecipient}.",
            message.Id,
            NotificationRecipientMasking.MaskPhone(user.PhoneNumber));

        return new NotificationSendResult(true, false, false, $"sim-sms-{message.Id:N}", null);
    }
}

public sealed class SimulatedWhatsAppNotificationProvider(
    IUserDirectory userDirectory,
    ILogger<SimulatedWhatsAppNotificationProvider> logger) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public async Task<NotificationSendResult> SendAsync(
        NotificationOutboxMessage message,
        ResolvedIntegrationConfiguration? integration,
        CancellationToken cancellationToken = default)
    {
        var users = await userDirectory.GetUsersAsync([message.RecipientUserId], cancellationToken);
        if (!users.TryGetValue(message.RecipientUserId, out var user) ||
            string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return new NotificationSendResult(false, false, false, null, "notifications.recipientPhoneMissing");
        }

        logger.LogInformation(
            "Simulated WhatsApp notification {MessageId} accepted for {MaskedRecipient}.",
            message.Id,
            NotificationRecipientMasking.MaskPhone(user.PhoneNumber));

        return new NotificationSendResult(true, false, false, $"sim-wa-{message.Id:N}", null);
    }
}

public sealed class SmtpEmailNotificationProvider(
    IUserDirectory userDirectory,
    ILogger<SmtpEmailNotificationProvider> logger) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(
        NotificationOutboxMessage message,
        ResolvedIntegrationConfiguration? integration,
        CancellationToken cancellationToken = default)
    {
        if (integration is null ||
            !string.Equals(integration.ProviderCode, IntegrationProviderCodes.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationSendResult(false, false, false, null, "notifications.invalidEmailIntegration");
        }

        EmailIntegrationSettings settings;
        try
        {
            settings = IntegrationSettingsSerializer.Deserialize<EmailIntegrationSettings>(integration.SettingsJson);
        }
        catch
        {
            return new NotificationSendResult(false, false, false, null, "notifications.invalidEmailSettings");
        }

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.SenderEmail) ||
            settings.Port is null or < 1 or > 65535)
        {
            return new NotificationSendResult(false, false, false, null, "notifications.emailConfigMissing");
        }

        var users = await userDirectory.GetUsersAsync([message.RecipientUserId], cancellationToken);
        if (!users.TryGetValue(message.RecipientUserId, out var user) ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return new NotificationSendResult(false, false, false, null, "notifications.recipientEmailMissing");
        }

        try
        {
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

            using var mail = new MailMessage
            {
                From = new MailAddress(settings.SenderEmail, settings.SenderName ?? "Schoolera"),
                Subject = message.Subject ?? "Schoolera",
                Body = message.BodyOrPayload,
                IsBodyHtml = false,
            };

            mail.To.Add(new MailAddress(user.Email, user.DisplayName));
            if (!string.IsNullOrWhiteSpace(settings.ReplyToEmail))
            {
                mail.ReplyToList.Add(new MailAddress(settings.ReplyToEmail));
            }

            await client.SendMailAsync(mail, cancellationToken);

            logger.LogInformation(
                "SMTP email notification {MessageId} accepted for {MaskedRecipient}.",
                message.Id,
                NotificationRecipientMasking.MaskEmail(user.Email));

            return new NotificationSendResult(true, false, false, $"smtp-{message.Id:N}", null);
        }
        catch (SmtpException)
        {
            return new NotificationSendResult(false, true, false, null, "notifications.smtpTransientFailure");
        }
        catch (Exception)
        {
            return new NotificationSendResult(false, true, false, null, "notifications.smtpSendFailure");
        }
    }
}

/// <summary>Routes Email channel delivery to Simulated or Smtp based on ProviderCode.</summary>
public sealed class NotificationChannelProviderRouter(
    SimulatedEmailNotificationProvider simulatedEmail,
    SmtpEmailNotificationProvider smtpEmail) : INotificationChannelProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public Task<NotificationSendResult> SendAsync(
        NotificationOutboxMessage message,
        ResolvedIntegrationConfiguration? integration,
        CancellationToken cancellationToken = default)
    {
        if (integration is null)
        {
            return Task.FromResult(new NotificationSendResult(
                false, false, false, null, "notifications.emailIntegrationMissing"));
        }

        if (IntegrationProviderCodes.IsSimulated(integration.ProviderCode))
        {
            return simulatedEmail.SendAsync(message, integration, cancellationToken);
        }

        if (string.Equals(integration.ProviderCode, IntegrationProviderCodes.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            if (!integration.IsConfigured)
            {
                return Task.FromResult(new NotificationSendResult(
                    false, false, false, null, "notifications.emailConfigInvalid"));
            }

            return smtpEmail.SendAsync(message, integration, cancellationToken);
        }

        return Task.FromResult(new NotificationSendResult(
            false, false, false, null, "notifications.unsupportedEmailProvider"));
    }
}

internal static class NotificationRecipientMasking
{
    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0)
        {
            return "***";
        }

        var local = email[..at];
        var domain = email[(at + 1)..];
        var maskedLocal = local.Length <= 1
            ? "*"
            : $"{local[0]}***";
        return $"{maskedLocal}@{domain}";
    }

    public static string MaskPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4)
        {
            return "****";
        }

        return $"***{digits[^4..]}";
    }
}
