namespace Schoolera.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Content-root-relative or absolute physical folder for uploaded files.
    /// </summary>
    public string StorageRoot { get; set; } = "App_Data/uploads";

    /// <summary>
    /// Public URL prefix served by ASP.NET Core static files, for example <c>/uploads</c>.
    /// </summary>
    public string PublicRequestPath { get; set; } = "/uploads";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];

    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
    ];
}
