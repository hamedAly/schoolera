namespace Schoolera.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrations { get; set; }

    public bool SeedData { get; set; }
}
