namespace Schoolera.Domain.Entities;

public sealed class School
{
    private School()
    {
    }

    public School(string name, string? city)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("School name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? City { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}