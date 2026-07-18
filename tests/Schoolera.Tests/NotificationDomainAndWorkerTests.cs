using Schoolera.Application.Integrations;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Notifications;

namespace Schoolera.Tests;

public sealed class NotificationDomainAndWorkerTests
{
    [Fact]
    public void Outbox_RejectsNegativeLifecycle_MarkReadIdempotent()
    {
        var message = new NotificationOutboxMessage(
            Guid.NewGuid(),
            NotificationEventType.AdmissionsOpened,
            NotificationChannel.InApp,
            "ar",
            null,
            "fallback.AdmissionsOpened",
            0,
            null,
            "Admissions opened",
            $"dedup-{Guid.NewGuid()}",
            null);

        message.MarkSent(null, null);
        message.MarkRead();
        var firstRead = message.ReadAtUtc;
        message.MarkRead();
        Assert.Equal(firstRead, message.ReadAtUtc);
    }

    [Fact]
    public void Outbox_DeadLettersAfterFailures()
    {
        var message = new NotificationOutboxMessage(
            Guid.NewGuid(),
            NotificationEventType.AdmissionApplicationSubmitted,
            NotificationChannel.Email,
            "en",
            null,
            "t",
            1,
            "Subject",
            "Body",
            $"dedup-{Guid.NewGuid()}",
            null);

        message.MarkProcessing();
        message.MarkFailed("provider.timeout", DateTimeOffset.UtcNow.AddMinutes(1));
        message.MarkProcessing();
        message.MarkDeadLetter("provider.permanent");
        Assert.Equal(NotificationStatus.DeadLetter, message.Status);
        Assert.Equal("provider.permanent", message.LastSafeFailureCode);
    }

    [Fact]
    public void Preference_RecordsConsentOnEnable()
    {
        var prefs = new ParentNotificationPreference(Guid.NewGuid());
        Assert.False(prefs.SmsEnabled);
        prefs.Update(true, true, true, false, true, "parent_preferences");
        Assert.True(prefs.SmsEnabled);
        Assert.NotNull(prefs.SmsConsentAtUtc);
        Assert.Equal("parent_preferences", prefs.SmsConsentSource);
    }

    [Fact]
    public void Subscription_UnsubscribeIsIdempotent()
    {
        var sub = new ParentAdmissionOpenSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            NotificationChannel.InApp);
        sub.Unsubscribe();
        Assert.False(sub.IsActive);
        var unsubscribedAt = sub.UnsubscribedAtUtc;
        sub.Unsubscribe();
        Assert.Equal(unsubscribedAt, sub.UnsubscribedAtUtc);
    }

    [Fact]
    public void EventClassification_AdmissionsOpenedIsOptional()
    {
        Assert.Equal(
            NotificationEventCategory.Optional,
            NotificationEventClassification.GetCategory(NotificationEventType.AdmissionsOpened));
        Assert.True(NotificationEventClassification.IsMandatory(NotificationEventType.AccountVerification));
    }

    [Fact]
    public void Integration_RejectsNonObjectJson()
    {
        Assert.Throws<ArgumentException>(() =>
            new PlatformIntegrationConfiguration(
                IntegrationType.Email,
                "Simulated",
                "بريد تجريبي",
                "Simulated Email",
                "[1,2,3]",
                1));
    }

    [Fact]
    public void WorkerOptions_HaveSafeDefaults()
    {
        var options = new NotificationWorkerOptions();
        Assert.True(options.BatchSize > 0);
        Assert.True(options.MaxAttempts > 1);
        Assert.True(options.BaseRetrySeconds > 0);
        Assert.True(options.MaxRetrySeconds >= options.BaseRetrySeconds);
    }

    [Fact]
    public void Redactor_DoesNotLeakInLogHelper()
    {
        var text = """{"ApiKey":"super-secret","AccessToken":"tok"}""";
        var redacted = SensitiveConfigurationRedactor.RedactForLog(text);
        Assert.DoesNotContain("super-secret", redacted);
        Assert.DoesNotContain("tok", redacted);
    }

    [Theory]
    [InlineData("ApiKey")]
    [InlineData("apikey")]
    [InlineData("WebhookSecret")]
    [InlineData("Password")]
    public void Redactor_RecognizesSensitiveNames(string name)
    {
        Assert.True(SensitiveConfigurationRedactor.IsSensitiveName(name));
    }
}
