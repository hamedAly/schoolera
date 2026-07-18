using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure.Storage;

namespace Schoolera.Tests;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorage _storage;
    private readonly FileStorageOptions _options;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "schoolera-uploads-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        _options = new FileStorageOptions
        {
            StorageRoot = _root,
            PublicRequestPath = "/uploads",
            MaxFileSizeBytes = 1024,
            AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"],
            AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"],
        };

        _storage = new LocalFileStorage(
            Options.Create(_options),
            NullLogger<LocalFileStorage>.Instance);
    }

    [Fact]
    public async Task SaveAsync_RejectsDisallowedExtension()
    {
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(new StoreFileRequest
            {
                Content = stream,
                OriginalFileName = "malware.exe",
                ContentType = "image/jpeg",
                Category = "schools/logos",
            }));

        Assert.Contains("extension", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_RejectsDisallowedContentType()
    {
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(new StoreFileRequest
            {
                Content = stream,
                OriginalFileName = "photo.jpg",
                ContentType = "application/octet-stream",
                Category = "schools/logos",
            }));

        Assert.Contains("Content type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_RejectsEmptyFile()
    {
        await using var stream = new MemoryStream();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(new StoreFileRequest
            {
                Content = stream,
                OriginalFileName = "photo.jpg",
                ContentType = "image/jpeg",
                Category = "schools/logos",
            }));

        Assert.Contains("Empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_RejectsOversizedFile()
    {
        var bytes = Enumerable.Repeat((byte)0xFF, 2048).ToArray();
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        await using var stream = new MemoryStream(bytes);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(new StoreFileRequest
            {
                Content = stream,
                OriginalFileName = "photo.jpg",
                ContentType = "image/jpeg",
                Category = "schools/logos",
            }));

        Assert.Contains("maximum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_RejectsPathTraversalCategory()
    {
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(new StoreFileRequest
            {
                Content = stream,
                OriginalFileName = "photo.jpg",
                ContentType = "image/jpeg",
                Category = "../secrets",
            }));

        Assert.Contains("category", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_RejectsMismatchedSignature()
    {
        await using var stream = new MemoryStream("not-an-image"u8.ToArray());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(new StoreFileRequest
            {
                Content = stream,
                OriginalFileName = "photo.png",
                ContentType = "image/png",
                Category = "schools/logos",
            }));

        Assert.Contains("signature", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WritesRelativePublicUrlAndSafeFileName()
    {
        // Minimal PNG signature + padding
        var png = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00,
        };
        await using var stream = new MemoryStream(png);

        var stored = await _storage.SaveAsync(new StoreFileRequest
        {
            Content = stream,
            OriginalFileName = "../../evil name.PNG",
            ContentType = "image/png",
            Category = "schools/logos",
        });

        Assert.StartsWith("/uploads/schools/logos/", stored.RelativePublicUrl, StringComparison.Ordinal);
        Assert.EndsWith(".png", stored.StoredFileName, StringComparison.Ordinal);
        Assert.DoesNotContain("..", stored.RelativePublicUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("evil", stored.StoredFileName, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(_root, "schools", "logos", stored.StoredFileName)));
    }

    [Fact]
    public async Task DeleteAsync_RejectsEscapeOutsideUploadRoot()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.DeleteAsync("/uploads/../secrets.txt"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingFileInsideRoot()
    {
        var png = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00,
        };
        await using var stream = new MemoryStream(png);

        var stored = await _storage.SaveAsync(new StoreFileRequest
        {
            Content = stream,
            OriginalFileName = "logo.png",
            ContentType = "image/png",
            Category = "schools/logos",
        });

        var physicalPath = Path.Combine(_root, "schools", "logos", stored.StoredFileName);
        Assert.True(File.Exists(physicalPath));

        await _storage.DeleteAsync(stored.RelativePublicUrl);

        Assert.False(File.Exists(physicalPath));
    }

    [Fact]
    public void GenerateSafeFileName_IsHexAndNonEmpty()
    {
        var name = LocalFileStorage.GenerateSafeFileName();

        Assert.Equal(32, name.Length);
        Assert.Matches("^[0-9a-f]+$", name);
    }

    [Fact]
    public void ResolvePhysicalPath_MapsPublicUrlUnderRoot()
    {
        var path = LocalFileStorage.ResolvePhysicalPath("/uploads/schools/logos/a.png", _options);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(_root, "schools", "logos", "a.png")),
            path);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
