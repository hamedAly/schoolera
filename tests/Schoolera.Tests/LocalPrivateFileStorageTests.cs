using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Infrastructure.Storage;

namespace Schoolera.Tests;

public sealed class LocalPrivateFileStorageTests : IDisposable
{
    private readonly string _root;
    private readonly LocalPrivateFileStorage _storage;
    private readonly PrivateFileStorageOptions _options;

    private static readonly byte[] PdfBytes = AdmissionTestHelpers.MinimalPdfBytes;
    private static readonly byte[] PngBytes =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] WebpBytes =
        [0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];

    public LocalPrivateFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "schoolera-private-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        _options = new PrivateFileStorageOptions
        {
            StorageRoot = _root,
            MaxFileSizeBytes = 4096,
            AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp"],
            AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png", "image/webp"],
        };

        _storage = new LocalPrivateFileStorage(
            Options.Create(_options),
            NullLogger<LocalPrivateFileStorage>.Instance);
    }

    [Theory]
    [InlineData("cr.pdf", "application/pdf")]
    [InlineData("id.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("scan.webp", "image/webp")]
    public async Task SaveAsync_AcceptsValidSignatures(string fileName, string contentType)
    {
        var bytes = SelectBytes(fileName);
        await using var stream = new MemoryStream(bytes);

        var stored = await _storage.SaveAsync(new PrivateFileStoreRequest
        {
            Content = stream,
            OriginalFileName = fileName,
            ContentType = contentType,
            Category = Guid.NewGuid().ToString("N"),
        });

        Assert.False(string.IsNullOrWhiteSpace(stored.StoredFileReference));
        Assert.DoesNotContain("..", stored.StoredFileReference, StringComparison.Ordinal);
        Assert.Equal(64, stored.Sha256Hash.Length);

        await using var read = await _storage.OpenReadAsync(stored.StoredFileReference);
        Assert.NotNull(read);
    }

    [Fact]
    public async Task SaveAsync_RejectsDisallowedExtension()
    {
        await using var stream = new MemoryStream(PdfBytes);

        var exception = await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "malware.exe",
                ContentType = "application/pdf",
                Category = "app",
            }));

        Assert.Equal(OnboardingErrorCodes.UnsupportedDocumentFormat, exception.ErrorCode);
    }

    [Fact]
    public async Task SaveAsync_RejectsContentTypeExtensionMismatch()
    {
        await using var stream = new MemoryStream(PdfBytes);

        var exception = await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "cr.pdf",
                ContentType = "image/png",
                Category = "app",
            }));

        Assert.Equal(OnboardingErrorCodes.UnsupportedDocumentFormat, exception.ErrorCode);
    }

    [Fact]
    public async Task SaveAsync_RejectsEmptyFile()
    {
        await using var stream = new MemoryStream();

        var exception = await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "cr.pdf",
                ContentType = "application/pdf",
                Category = "app",
            }));

        Assert.Equal(OnboardingErrorCodes.EmptyDocument, exception.ErrorCode);
    }

    [Fact]
    public async Task SaveAsync_RejectsOversizedFile()
    {
        var bytes = new byte[8192];
        PdfBytes.CopyTo(bytes, 0);
        await using var stream = new MemoryStream(bytes);

        var exception = await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "cr.pdf",
                ContentType = "application/pdf",
                Category = "app",
            }));

        Assert.Equal(OnboardingErrorCodes.DocumentTooLarge, exception.ErrorCode);
    }

    [Fact]
    public async Task SaveAsync_RejectsMismatchedSignature()
    {
        await using var stream = new MemoryStream("this is definitely not a pdf"u8.ToArray());

        var exception = await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "cr.pdf",
                ContentType = "application/pdf",
                Category = "app",
            }));

        Assert.Equal(OnboardingErrorCodes.InvalidFileSignature, exception.ErrorCode);
    }

    [Fact]
    public async Task SaveAsync_RejectsPathTraversalCategory()
    {
        await using var stream = new MemoryStream(PdfBytes);

        await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "cr.pdf",
                ContentType = "application/pdf",
                Category = "../secrets",
            }));
    }

    [Fact]
    public async Task SaveThenDelete_RemovesFile()
    {
        await using var stream = new MemoryStream(PdfBytes);
        var stored = await _storage.SaveAsync(new PrivateFileStoreRequest
        {
            Content = stream,
            OriginalFileName = "cr.pdf",
            ContentType = "application/pdf",
            Category = "app",
        });

        await _storage.DeleteAsync(stored.StoredFileReference);

        var read = await _storage.OpenReadAsync(stored.StoredFileReference);
        Assert.Null(read);
    }

    [Fact]
    public void ResolvePhysicalPath_RejectsTraversalReference()
    {
        Assert.Throws<InvalidOperationException>(() =>
            LocalPrivateFileStorage.ResolvePhysicalPath("../secrets.txt", _root));
    }

    [Fact]
    public void GenerateSafeFileName_IsHexAndNonEmpty()
    {
        var name = LocalPrivateFileStorage.GenerateSafeFileName();

        Assert.Equal(32, name.Length);
        Assert.Matches("^[0-9a-f]+$", name);
    }

    private static byte[] SelectBytes(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => PdfBytes,
            ".png" => PngBytes,
            ".jpg" or ".jpeg" => JpegBytes,
            ".webp" => WebpBytes,
            _ => [],
        };

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
