namespace Schoolera.Application.SchoolOnboarding.Common;

public static class OnboardingDownloadName
{
    /// <summary>Produces a safe, path-free download file name for Content-Disposition.</summary>
    public static string Sanitize(string? originalFileName)
    {
        var name = Path.GetFileName(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name))
        {
            return "document";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "document" : cleaned;
    }
}
