using Microsoft.Extensions.Options;

namespace Schoolera.Infrastructure.Storage;

public sealed class FileStorageOptionsValidator : IValidateOptions<FileStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, FileStorageOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.StorageRoot))
        {
            failures.Add("FileStorage:StorageRoot is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicRequestPath) ||
            !options.PublicRequestPath.StartsWith("/", StringComparison.Ordinal))
        {
            failures.Add("FileStorage:PublicRequestPath must start with '/'.");
        }

        if (options.MaxFileSizeBytes <= 0)
        {
            failures.Add("FileStorage:MaxFileSizeBytes must be greater than zero.");
        }

        if (options.AllowedExtensions is null || options.AllowedExtensions.Length == 0)
        {
            failures.Add("FileStorage:AllowedExtensions must contain at least one extension.");
        }
        else if (options.AllowedExtensions.Any(extension =>
                     string.IsNullOrWhiteSpace(extension) ||
                     !extension.StartsWith(".", StringComparison.Ordinal)))
        {
            failures.Add("FileStorage:AllowedExtensions entries must start with '.'.");
        }

        if (options.AllowedContentTypes is null || options.AllowedContentTypes.Length == 0)
        {
            failures.Add("FileStorage:AllowedContentTypes must contain at least one content type.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
