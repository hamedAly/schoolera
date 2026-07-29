using System.Text.Json;

namespace Schoolera.E2E.Infrastructure;

public static class E2EConfig
{
    private static readonly Lazy<E2ESettings> Settings = new(Load);

    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL")
        ?? Settings.Value.BaseUrl
        ?? "http://localhost:5100";

    public static string SeedPassword =>
        Environment.GetEnvironmentVariable("E2E_SEED_PASSWORD")
        ?? throw new InvalidOperationException(
            "E2E_SEED_PASSWORD is required. Use the same value as Auth:SeedUsers:DefaultPassword.");

    public static int DefaultTimeoutMs => Settings.Value.DefaultTimeoutMs;

    public static int NavigationTimeoutMs => Settings.Value.NavigationTimeoutMs;

    public static string PublishedSchoolSlug =>
        Settings.Value.PublishedSchoolSlug ?? "cairo-international-school";

    public static bool Headed =>
        string.Equals(Environment.GetEnvironmentVariable("HEADED"), "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("HEADED"), "true", StringComparison.OrdinalIgnoreCase);

    public static string AuthDirectory
    {
        get
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".auth");
            Directory.CreateDirectory(dir);
            return Path.GetFullPath(dir);
        }
    }

    private static E2ESettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.E2E.json");
        if (!File.Exists(path))
        {
            return new E2ESettings();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<E2ESettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? new E2ESettings();
    }

    private sealed class E2ESettings
    {
        public string? BaseUrl { get; set; }
        public int DefaultTimeoutMs { get; set; } = 30_000;
        public int NavigationTimeoutMs { get; set; } = 45_000;
        public string? PublishedSchoolSlug { get; set; }
    }
}
