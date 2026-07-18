namespace Schoolera.Infrastructure.Storage;

/// <summary>
/// Options for private (non-public) onboarding document storage. The storage root is never
/// exposed by static-file middleware and files never receive public URLs.
/// </summary>
public sealed class PrivateFileStorageOptions
{
    public const string SectionName = "PrivateFileStorage";

    /// <summary>Content-root-relative or absolute physical folder for private documents.</summary>
    public string StorageRoot { get; set; } = "App_Data/private/school-onboarding";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [".pdf", ".jpg", ".jpeg", ".png", ".webp"];

    public string[] AllowedContentTypes { get; set; } =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
    ];
}
