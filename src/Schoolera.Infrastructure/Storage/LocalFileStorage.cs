using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Infrastructure.Storage;

public sealed partial class LocalFileStorage(
    IOptions<FileStorageOptions> options,
    ILogger<LocalFileStorage> logger) : IFileStorage
{
    private static readonly Dictionary<string, byte[][]> SignaturesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = [[0xFF, 0xD8, 0xFF]],
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
            [".webp"] =
            [
                // RIFF....WEBP
                [0x52, 0x49, 0x46, 0x46],
            ],
        };

    public async Task<StoredFile> SaveAsync(
        StoreFileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        var settings = options.Value;
        var extension = NormalizeExtension(Path.GetExtension(request.OriginalFileName));
        var contentType = (request.ContentType ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension) ||
            !settings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"File extension '{extension}' is not allowed.");
        }

        if (!settings.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed.");
        }

        if (!IsExtensionCompatibleWithContentType(extension, contentType))
        {
            throw new InvalidOperationException(
                $"Content type '{contentType}' does not match extension '{extension}'.");
        }

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        await using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("Empty files are not allowed.");
        }

        if (bytes.Length > settings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File exceeds the maximum allowed size of {settings.MaxFileSizeBytes} bytes.");
        }

        ValidateFileSignature(extension, bytes);

        var category = SanitizeCategory(request.Category);
        var storedFileName = $"{GenerateSafeFileName()}{extension}";
        var categoryDirectory = Path.Combine(settings.StorageRoot, category);
        Directory.CreateDirectory(categoryDirectory);

        var physicalPath = Path.Combine(categoryDirectory, storedFileName);
        EnsurePathInsideRoot(physicalPath, settings.StorageRoot);

        await File.WriteAllBytesAsync(physicalPath, bytes, cancellationToken);

        var relativePublicUrl =
            $"{settings.PublicRequestPath.TrimEnd('/')}/{category.Replace('\\', '/')}/{storedFileName}";

        logger.LogInformation(
            "Stored uploaded file {StoredFileName} under category {Category} ({SizeBytes} bytes).",
            storedFileName,
            category,
            bytes.Length);

        return new StoredFile(relativePublicUrl, storedFileName, bytes.Length, contentType);
    }

    public Task DeleteAsync(string relativePublicUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var physicalPath = ResolvePhysicalPath(relativePublicUrl, settings);
        EnsurePathInsideRoot(physicalPath, settings.StorageRoot);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
            logger.LogInformation("Deleted uploaded file at public URL {RelativePublicUrl}.", relativePublicUrl);
        }
        else
        {
            logger.LogInformation(
                "Delete skipped; file not found for public URL {RelativePublicUrl}.",
                relativePublicUrl);
        }

        return Task.CompletedTask;
    }

    internal static string SanitizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("File category is required.");
        }

        var normalized = category
            .Replace('\\', '/')
            .Trim()
            .Trim('/');

        if (normalized.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalized) ||
            !CategoryRegex().IsMatch(normalized))
        {
            throw new InvalidOperationException("File category contains invalid characters or path segments.");
        }

        return normalized.Replace('/', Path.DirectorySeparatorChar);
    }

    internal static string GenerateSafeFileName()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    internal static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }

    internal static bool IsExtensionCompatibleWithContentType(string extension, string contentType)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => contentType is "image/jpeg",
            ".png" => contentType is "image/png",
            ".webp" => contentType is "image/webp",
            _ => false,
        };
    }

    internal static void ValidateFileSignature(string extension, byte[] bytes)
    {
        if (!SignaturesByExtension.TryGetValue(extension, out var signatures))
        {
            throw new InvalidOperationException($"No signature rules configured for '{extension}'.");
        }

        var matched = signatures.Any(signature =>
            bytes.Length >= signature.Length &&
            bytes.AsSpan(0, signature.Length).SequenceEqual(signature));

        if (!matched)
        {
            throw new InvalidOperationException(
                $"File content does not match the expected signature for '{extension}'.");
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            // RIFF....WEBP — bytes 8-11 must be WEBP
            if (bytes.Length < 12 ||
                bytes[8] != (byte)'W' ||
                bytes[9] != (byte)'E' ||
                bytes[10] != (byte)'B' ||
                bytes[11] != (byte)'P')
            {
                throw new InvalidOperationException("File content does not match the expected WEBP signature.");
            }
        }
    }

    internal static string ResolvePhysicalPath(string relativePublicUrl, FileStorageOptions settings)
    {
        if (string.IsNullOrWhiteSpace(relativePublicUrl))
        {
            throw new InvalidOperationException("A relative public URL is required for deletion.");
        }

        var publicPrefix = settings.PublicRequestPath.TrimEnd('/');
        var normalizedUrl = relativePublicUrl.Replace('\\', '/').Trim();

        if (!normalizedUrl.StartsWith(publicPrefix + "/", StringComparison.OrdinalIgnoreCase) &&
            !normalizedUrl.Equals(publicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The provided URL is outside the configured uploads public path.");
        }

        var relative = normalizedUrl[publicPrefix.Length..].TrimStart('/');
        if (relative.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("The provided URL contains invalid path segments.");
        }

        return Path.GetFullPath(Path.Combine(settings.StorageRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
    }

    internal static void EnsurePathInsideRoot(string physicalPath, string storageRoot)
    {
        var fullPath = Path.GetFullPath(physicalPath);
        var fullRoot = Path.GetFullPath(storageRoot)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Path.GetFullPath(physicalPath), Path.GetFullPath(storageRoot), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved path escapes the configured upload root.");
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+([/_-][a-zA-Z0-9]+)*$")]
    private static partial Regex CategoryRegex();
}
