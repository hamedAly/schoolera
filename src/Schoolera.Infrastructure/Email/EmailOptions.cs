namespace Schoolera.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// <c>DevelopmentLog</c> (Development only) or <c>Smtp</c>.
    /// </summary>
    public string Mode { get; set; } = "DevelopmentLog";

    public string FromName { get; set; } = "Schoolera";

    public string FromAddress { get; set; } = "no-reply@schoolera.local";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public static class EmailDeliveryModes
{
    public const string DevelopmentLog = "DevelopmentLog";
    public const string Smtp = "Smtp";
}
