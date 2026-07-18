namespace Schoolera.Domain.Entities;

/// <summary>Parent notification channel preferences and consent timestamps.</summary>
public sealed class ParentNotificationPreference
{
    private ParentNotificationPreference()
    {
    }

    public ParentNotificationPreference(Guid parentUserId)
    {
        Id = Guid.NewGuid();
        ParentUserId = parentUserId;
        InAppEnabled = true;
        EmailEnabled = true;
        SmsEnabled = false;
        WhatsAppEnabled = false;
        OptionalAdmissionsOpenEnabled = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ParentUserId { get; private set; }

    public bool InAppEnabled { get; private set; }

    public bool EmailEnabled { get; private set; }

    public bool SmsEnabled { get; private set; }

    public bool WhatsAppEnabled { get; private set; }

    public DateTimeOffset? EmailConsentAtUtc { get; private set; }

    public DateTimeOffset? SmsConsentAtUtc { get; private set; }

    public DateTimeOffset? WhatsAppConsentAtUtc { get; private set; }

    public string? EmailConsentSource { get; private set; }

    public string? SmsConsentSource { get; private set; }

    public string? WhatsAppConsentSource { get; private set; }

    public bool OptionalAdmissionsOpenEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(
        bool inAppEnabled,
        bool emailEnabled,
        bool smsEnabled,
        bool whatsAppEnabled,
        bool optionalAdmissionsOpenEnabled,
        string? consentSource)
    {
        InAppEnabled = inAppEnabled;
        OptionalAdmissionsOpenEnabled = optionalAdmissionsOpenEnabled;
        SetEmail(emailEnabled, consentSource);
        SetSms(smsEnabled, consentSource);
        SetWhatsApp(whatsAppEnabled, consentSource);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetEmail(bool enabled, string? source)
    {
        if (enabled && !EmailEnabled)
        {
            EmailConsentAtUtc = DateTimeOffset.UtcNow;
            EmailConsentSource = Truncate(source, 100) ?? "parent_preferences";
        }
        else if (!enabled)
        {
            EmailConsentAtUtc = null;
            EmailConsentSource = null;
        }

        EmailEnabled = enabled;
    }

    private void SetSms(bool enabled, string? source)
    {
        if (enabled && !SmsEnabled)
        {
            SmsConsentAtUtc = DateTimeOffset.UtcNow;
            SmsConsentSource = Truncate(source, 100) ?? "parent_preferences";
        }
        else if (!enabled)
        {
            SmsConsentAtUtc = null;
            SmsConsentSource = null;
        }

        SmsEnabled = enabled;
    }

    private void SetWhatsApp(bool enabled, string? source)
    {
        if (enabled && !WhatsAppEnabled)
        {
            WhatsAppConsentAtUtc = DateTimeOffset.UtcNow;
            WhatsAppConsentSource = Truncate(source, 100) ?? "parent_preferences";
        }
        else if (!enabled)
        {
            WhatsAppConsentAtUtc = null;
            WhatsAppConsentSource = null;
        }

        WhatsAppEnabled = enabled;
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
