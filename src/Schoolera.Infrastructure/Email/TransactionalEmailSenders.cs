using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Infrastructure.Email;

public sealed class DevelopmentLogEmailSender(
    IOptions<EmailOptions> emailOptions,
    IHostEnvironment hostEnvironment,
    ILogger<DevelopmentLogEmailSender> logger) : ITransactionalEmailSender
{
    public string Mode => EmailDeliveryModes.DevelopmentLog;

    public Task SendVerificationCodeAsync(
        VerificationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Email:Mode=DevelopmentLog is not allowed outside the Development environment.");
        }

        var from = emailOptions.Value.FromAddress;
        logger.LogWarning(
            "[DevelopmentLog email] Verification code generated for {RecipientEmail} (from {FromAddress}). " +
            "Code={VerificationCode}; expires in {ExpirationMinutes} minutes. " +
            "This mode does not send a real email — copy the code from this API console log.",
            message.RecipientEmail,
            from,
            message.VerificationCode,
            message.ExpirationMinutes);

        return Task.CompletedTask;
    }
}

public sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : ITransactionalEmailSender
{
    public string Mode => EmailDeliveryModes.Smtp;

    public async Task SendVerificationCodeAsync(
        VerificationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var options = emailOptions.Value;
        var smtp = options.Smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException("Email:Smtp:Host is required when Email:Mode=Smtp.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException("Email:FromAddress is required when Email:Mode=Smtp.");
        }

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        }

        using var mail = BuildMessage(options, message);
        await client.SendMailAsync(mail, cancellationToken);

        logger.LogInformation(
            "Verification email accepted by SMTP for {RecipientEmail} via {SmtpHost}:{SmtpPort}.",
            message.RecipientEmail,
            smtp.Host,
            smtp.Port);
    }

    private static MailMessage BuildMessage(EmailOptions options, VerificationEmailMessage message)
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

        return new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
            To = { new MailAddress(message.RecipientEmail, displayName) },
        };
    }
}

public sealed class EmailOptionsStartupValidator : IValidateOptions<EmailOptions>
{
    private readonly IHostEnvironment _environment;

    public EmailOptionsStartupValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var mode = options.Mode?.Trim() ?? string.Empty;

        // Provider credentials live in PlatformIntegrationConfiguration.SettingsJson.
        // Email appsettings remain for backward compatibility and are not required for delivery.
        if (string.Equals(mode, EmailDeliveryModes.DevelopmentLog, StringComparison.OrdinalIgnoreCase))
        {
            if (!_environment.IsDevelopment())
            {
                return ValidateOptionsResult.Fail(
                    "Email:Mode=DevelopmentLog is only allowed in Development.");
            }

            return ValidateOptionsResult.Success;
        }

        if (string.Equals(mode, EmailDeliveryModes.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            // Development must not require Smtp credentials from appsettings.
            // Non-Development also skips Host/FromAddress checks — DB integration settings are authoritative.
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            $"Email:Mode '{options.Mode}' is not supported. Use DevelopmentLog or Smtp.");
    }
}
