namespace Schoolera.Domain.Entities;

/// <summary>Public interest/contact lead submitted from a school profile.</summary>
public sealed class SchoolContactLead
{
    private SchoolContactLead()
    {
    }

    public SchoolContactLead(
        Guid schoolId,
        string source,
        string name,
        string phone,
        string? email,
        string? message,
        string culture)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        Source = source.Trim();
        Name = name.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        ConsentAccepted = true;
        Culture = culture.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public string Source { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Message { get; private set; }

    public bool ConsentAccepted { get; private set; }

    public string Culture { get; private set; } = "ar";

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
