using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolOnboarding.Constants;

namespace Schoolera.Infrastructure.Storage;

/// <summary>
/// Local disk implementation of <see cref="IPrivateFileStorage"/>. Files are stored under a
/// private root that is never registered with static-file middleware. Stored references are
/// opaque relative paths (never physical paths or public URLs).
/// </summary>
public sealed partial class LocalPrivateFileStorage(
    IOptions<PrivateFileStorageOptions> options,
    ILogger<LocalPrivateFileStorage> logger) : IPrivateFileStorage
{
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffSignature = [0x52, 0x49, 0x46, 0x46]; // RIFF

    public PrivateFileStoragePolicy Policy => new(
        options.Value.MaxFileSizeBytes,
        options.Value.AllowedExtensions,
        options.Value.AllowedContentTypes);

    public async Task<StoredPrivateFile> SaveAsync(
        PrivateFileStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        var settings = options.Value;
        var extension = NormalizeExtension(Path.GetExtension(request.OriginalFileName));
        var contentType = (request.ContentType ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension) ||
            !settings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
            !settings.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase) ||
            !IsExtensionCompatibleWithContentType(extension, contentType))
        {
            throw new PrivateFileValidationException(
                OnboardingErrorCodes.UnsupportedDocumentFormat,
                "The uploaded file format is not supported.");
        }

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        if (bytes.Length == 0)
        {
            throw new PrivateFileValidationException(
                OnboardingErrorCodes.EmptyDocument,
                "The uploaded file is empty.");
        }

        if (bytes.Length > settings.MaxFileSizeBytes)
        {
            throw new PrivateFileValidationException(
                OnboardingErrorCodes.DocumentTooLarge,
                $"The uploaded file exceeds the maximum allowed size of {settings.MaxFileSizeBytes} bytes.");
        }

        if (!IsValidSignature(extension, bytes))
        {
            throw new PrivateFileValidationException(
                OnboardingErrorCodes.InvalidFileSignature,
                "The uploaded file content does not match its declared type.");
        }

        var category = SanitizeCategory(request.Category);
        var storedFileName = $"{GenerateSafeFileName()}{extension}";
        var categoryDirectory = Path.Combine(settings.StorageRoot, category);
        Directory.CreateDirectory(categoryDirectory);

        var physicalPath = Path.Combine(categoryDirectory, storedFileName);
        EnsurePathInsideRoot(physicalPath, settings.StorageRoot);
        var tempPath = physicalPath + ".tmp";

        try
        {
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            File.Move(tempPath, physicalPath, overwrite: true);
        }
        catch
        {
            TryDeleteQuietly(tempPath);
            TryDeleteQuietly(physicalPath);
            throw;
        }

        var storedReference = $"{category.Replace('\\', '/')}/{storedFileName}";
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        logger.LogInformation(
            "Stored private onboarding file under {Category} ({SizeBytes} bytes).",
            category,
            bytes.Length);

        return new StoredPrivateFile(storedReference, storedFileName, bytes.Length, contentType, hash);
    }

    public Task<Stream?> OpenReadAsync(
        string storedFileReference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var physicalPath = ResolvePhysicalPath(storedFileReference, settings.StorageRoot);

        if (!File.Exists(physicalPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storedFileReference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var physicalPath = ResolvePhysicalPath(storedFileReference, settings.StorageRoot);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
            logger.LogInformation("Deleted private onboarding file {StoredReference}.", storedFileReference);
        }

        return Task.CompletedTask;
    }

    internal static string SanitizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new PrivateFileValidationException(
                OnboardingErrorCodes.UnsupportedDocumentFormat,
                "A storage category is required.");
        }

        var normalized = category.Replace('\\', '/').Trim().Trim('/');

        if (normalized.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalized) ||
            !CategoryRegex().IsMatch(normalized))
        {
            throw new PrivateFileValidationException(
                OnboardingErrorCodes.UnsupportedDocumentFormat,
                "The storage category contains invalid characters or path segments.");
        }

        return normalized.Replace('/', Path.DirectorySeparatorChar);
    }

    internal static string GenerateSafeFileName() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

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

    internal static bool IsExtensionCompatibleWithContentType(string extension, string contentType) =>
        extension switch
        {
            ".pdf" => contentType is "application/pdf",
            ".jpg" or ".jpeg" => contentType is "image/jpeg",
            ".png" => contentType is "image/png",
            ".webp" => contentType is "image/webp",
            _ => false,
        };

    internal static bool IsValidSignature(string extension, byte[] bytes) =>
        extension switch
        {
            ".pdf" => IsValidPdf(bytes),
            ".jpg" or ".jpeg" => StartsWith(bytes, JpegSignature),
            ".png" => StartsWith(bytes, PngSignature),
            ".webp" => StartsWith(bytes, RiffSignature) && bytes.Length >= 12 &&
                       bytes[8] == (byte)'W' && bytes[9] == (byte)'E' &&
                       bytes[10] == (byte)'B' && bytes[11] == (byte)'P',
            _ => false,
        };

    /// <summary>
    /// Rejects zero-page / header-only stubs (e.g. <c>%PDF-</c> alone or <c>/Kids[]/Count 0</c>
    /// without a <c>/Type/Page</c> object) that open with no pages in PDF readers.
    /// </summary>
    internal static bool IsValidPdf(byte[] bytes)
    {
        if (!StartsWith(bytes, PdfSignature))
        {
            return false;
        }

        var ascii = Encoding.ASCII.GetString(bytes);
        if (!ascii.Contains("%%EOF", StringComparison.Ordinal))
        {
            return false;
        }

        // Require a Page object; /Type/Pages alone (empty Kids/Count 0) is not enough.
        return PdfPageObjectRegex().IsMatch(ascii);
    }

    private static bool StartsWith(byte[] bytes, byte[] signature) =>
        bytes.Length >= signature.Length &&
        bytes.AsSpan(0, signature.Length).SequenceEqual(signature);

    private static void TryDeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup after a failed write; ignore secondary failures.
        }
    }

    internal static string ResolvePhysicalPath(string storedFileReference, string storageRoot)
    {
        if (string.IsNullOrWhiteSpace(storedFileReference))
        {
            throw new InvalidOperationException("A stored file reference is required.");
        }

        var relative = storedFileReference.Replace('\\', '/').Trim().TrimStart('/');
        if (relative.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("The stored file reference contains invalid path segments.");
        }

        var physicalPath = Path.GetFullPath(
            Path.Combine(storageRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        EnsurePathInsideRoot(physicalPath, storageRoot);
        return physicalPath;
    }

    internal static void EnsurePathInsideRoot(string physicalPath, string storageRoot)
    {
        var fullPath = Path.GetFullPath(physicalPath);
        var fullRoot = Path.GetFullPath(storageRoot)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved path escapes the configured private storage root.");
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+([/_-][a-zA-Z0-9]+)*$")]
    private static partial Regex CategoryRegex();

    [GeneratedRegex(@"\/Type\s*\/Page(?![A-Za-z])")]
    private static partial Regex PdfPageObjectRegex();
}
