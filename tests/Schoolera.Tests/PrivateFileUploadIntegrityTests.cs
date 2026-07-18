using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;
using Schoolera.Infrastructure.Storage;

namespace Schoolera.Tests;

/// <summary>
/// Focused coverage for corrupted/placeholder private PDF prevention and file↔DB rollback.
/// </summary>
public sealed class PrivateFileUploadIntegrityTests : IDisposable
{
    private readonly string _root;
    private readonly LocalPrivateFileStorage _storage;

    private static readonly byte[] EmptyPageStubPdf = Encoding.ASCII.GetBytes(
        """
        %PDF-1.4
        1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
        2 0 obj<</Type/Pages/Kids[]/Count 0>>endobj
        trailer<</Root 1 0 R>>
        %%EOF
        """);

    private static readonly byte[] HeaderOnlyPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n%vault\n");

    public PrivateFileUploadIntegrityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "schoolera-private-integrity", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _storage = new LocalPrivateFileStorage(
            Options.Create(new PrivateFileStorageOptions
            {
                StorageRoot = _root,
                MaxFileSizeBytes = 10 * 1024,
                AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp"],
                AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png", "image/webp"],
            }),
            NullLogger<LocalPrivateFileStorage>.Instance);
    }

    [Fact]
    public async Task SaveAsync_RejectsEmptyByteStream_WithoutCreatingFile()
    {
        await using var stream = new MemoryStream();
        var exception = await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "empty.pdf",
                ContentType = "application/pdf",
                Category = "empty-check",
            }));

        Assert.Equal(OnboardingErrorCodes.EmptyDocument, exception.ErrorCode);
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [MemberData(nameof(InvalidPdfPayloads))]
    public async Task SaveAsync_RejectsInvalidOrEmptyPagePdfs_WithoutCreatingFile(byte[] bytes)
    {
        await using var stream = new MemoryStream(bytes);
        var exception = await Assert.ThrowsAsync<PrivateFileValidationException>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "bad.pdf",
                ContentType = "application/pdf",
                Category = "invalid-pdf",
            }));

        Assert.Equal(OnboardingErrorCodes.InvalidFileSignature, exception.ErrorCode);
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    public static TheoryData<byte[]> InvalidPdfPayloads =>
        new()
        {
            EmptyPageStubPdf,
            HeaderOnlyPdf,
            Encoding.ASCII.GetBytes("%PDF-1.4\ntrailer\n"),
            "not-a-pdf"u8.ToArray(),
        };

    [Fact]
    public async Task SaveAsync_AcceptsValidOnePagePdf()
    {
        await using var stream = new MemoryStream(AdmissionTestHelpers.MinimalPdfBytes);
        var stored = await _storage.SaveAsync(new PrivateFileStoreRequest
        {
            Content = stream,
            OriginalFileName = "valid.pdf",
            ContentType = "application/pdf",
            Category = "valid",
        });

        Assert.True(File.Exists(Path.Combine(_root, stored.StoredFileReference.Replace('/', Path.DirectorySeparatorChar))));
        Assert.True(stored.SizeBytes > 0);
    }

    [Fact]
    public async Task SaveAsync_WhenWriteFails_LeavesNoPartialOrTempFile()
    {
        var blocker = Path.Combine(_root, "blocked-cat");
        await File.WriteAllTextAsync(blocker, "not-a-directory");

        await using var stream = new MemoryStream(AdmissionTestHelpers.MinimalPdfBytes);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _storage.SaveAsync(new PrivateFileStoreRequest
            {
                Content = stream,
                OriginalFileName = "valid.pdf",
                ContentType = "application/pdf",
                Category = "blocked-cat",
            }));

        Assert.DoesNotContain(
            Directory.GetFiles(_root, "*", SearchOption.AllDirectories),
            path => path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AdmissionApplicationSeeder_DoesNotDependOnPrivateFileStorage()
    {
        var ctor = typeof(AdmissionApplicationSeeder).GetConstructors().Single();
        Assert.DoesNotContain(
            ctor.GetParameters(),
            parameter => parameter.ParameterType == typeof(IPrivateFileStorage));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class PrivateFileUploadIntegrityIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly SchooleraWebApplicationFactory _factory;

    public PrivateFileUploadIntegrityIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Attachment_WithoutFile_CreatesNoPhysicalFileOrDbRow()
    {
        await Gate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            long beforeCount;
            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                beforeCount = await db.AdmissionApplicationAttachments
                    .CountAsync(item => item.AdmissionApplicationId == applicationId);
            }

            var storageRoot = ResolvePrivateRoot();
            var beforeFiles = CountFiles(storageRoot);

            using var content = new MultipartFormDataContent();
            content.Add(
                new StringContent(((int)AdmissionAttachmentType.SupportingDocument).ToString()),
                "attachmentType");
            var response = await client.PostAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.AttachmentTypeInvalid);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                var afterCount = await db.AdmissionApplicationAttachments
                    .CountAsync(item => item.AdmissionApplicationId == applicationId);
                Assert.Equal(beforeCount, afterCount);
            }

            Assert.Equal(beforeFiles, CountFiles(storageRoot));
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task Attachment_EmptyPageStubPdf_IsRejected_WithoutDbOrFile()
    {
        await Gate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var stub = Encoding.ASCII.GetBytes(
                """
                %PDF-1.4
                1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
                2 0 obj<</Type/Pages/Kids[]/Count 0>>endobj
                trailer<</Root 1 0 R>>
                %%EOF
                """);

            var storageRoot = ResolvePrivateRoot();
            var beforeFiles = CountFiles(storageRoot);

            using var upload = AdmissionTestHelpers.BuildPdfUploadContent(stub);
            var response = await client.PostAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                upload);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.AttachmentTypeInvalid);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                Assert.False(await db.AdmissionApplicationAttachments
                    .AnyAsync(item => item.AdmissionApplicationId == applicationId));
            }

            Assert.Equal(beforeFiles, CountFiles(storageRoot));
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task Attachment_ValidPdf_PersistsFileAndDbRow()
    {
        await Gate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            using var upload = AdmissionTestHelpers.BuildPdfUploadContent(AdmissionTestHelpers.MinimalPdfBytes);
            var response = await client.PostAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                upload);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string storageKey;
            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                var attachment = await db.AdmissionApplicationAttachments
                    .AsNoTracking()
                    .SingleAsync(item => item.AdmissionApplicationId == applicationId);
                storageKey = attachment.StorageKey;
                Assert.True(attachment.FileSizeBytes > 100);
            }

            await using var scope2 = _factory.Services.CreateAsyncScope();
            var storage = scope2.ServiceProvider.GetRequiredService<IPrivateFileStorage>();
            await using var stream = await storage.OpenReadAsync(storageKey);
            Assert.NotNull(stream);
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task ChildDocument_DbSaveFailure_RemovesOrphanFile()
    {
        await Gate.WaitAsync();
        try
        {
            var privateRoot = Path.Combine(
                Path.GetTempPath(),
                "schoolera-private-orphan",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(privateRoot);
            DbFailSwitch.Enabled = false;

            await using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["PrivateFileStorage:StorageRoot"] = privateRoot,
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IUnitOfWork>();
                    services.AddScoped<IUnitOfWork>(sp =>
                        new ConditionalThrowingUnitOfWork(
                            new UnitOfWork(sp.GetRequiredService<SchooleraDbContext>())));
                });
            });

            using var client = AuthTestHelpers.CreateCookieClient(factory);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                client,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);

            var childId = await AdmissionTestHelpers.CreateChildAsync(client);

            DbFailSwitch.Enabled = true;
            try
            {
                using var content = new MultipartFormDataContent();
                var file = new ByteArrayContent(AdmissionTestHelpers.MinimalPdfBytes);
                file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                content.Add(file, "file", "birth.pdf");
                content.Add(
                    new StringContent(((int)ChildDocumentType.BirthCertificate).ToString()),
                    "documentType");

                var response = await client.PostAsync(
                    $"/api/parent/children/{childId}/documents",
                    content);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }
            finally
            {
                DbFailSwitch.Enabled = false;
            }

            Assert.Empty(Directory.GetFiles(privateRoot, "*", SearchOption.AllDirectories));

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                Assert.False(await db.ChildDocuments.AnyAsync(d => d.ChildProfileId == childId));
            }
        }
        finally
        {
            DbFailSwitch.Enabled = false;
            Gate.Release();
        }
    }

    [Fact]
    public void SeededPrivateApiRoot_ContainsNoCorruptedPlaceholderPdfs()
    {
        var apiPrivate = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Schoolera.Api", "App_Data", "private"));

        if (!Directory.Exists(apiPrivate))
        {
            return;
        }

        var corrupted = Directory.GetFiles(apiPrivate, "*.pdf", SearchOption.AllDirectories)
            .Where(IsCorruptedPlaceholderPdf)
            .ToList();

        Assert.True(
            corrupted.Count == 0,
            $"Found {corrupted.Count} corrupted placeholder PDF(s) under App_Data/private. First: {corrupted.FirstOrDefault()}");
    }

    private string ResolvePrivateRoot()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOptions<PrivateFileStorageOptions>>().Value.StorageRoot;
    }

    private static int CountFiles(string root) =>
        Directory.Exists(root)
            ? Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length
            : 0;

    private static bool IsCorruptedPlaceholderPdf(string path)
    {
        var info = new FileInfo(path);
        if (info.Length == 0 || info.Length > 512)
        {
            return false;
        }

        var text = File.ReadAllText(path);
        if (!text.StartsWith("%PDF-", StringComparison.Ordinal))
        {
            return true;
        }

        if (text.Contains("/Kids[]", StringComparison.Ordinal) ||
            text.Contains("/Count 0", StringComparison.Ordinal) ||
            !text.Contains("%%EOF", StringComparison.Ordinal))
        {
            return true;
        }

        return !System.Text.RegularExpressions.Regex.IsMatch(text, @"\/Type\s*\/Page(?![A-Za-z])");
    }

    private static class DbFailSwitch
    {
        public static volatile bool Enabled;
    }

    private sealed class ConditionalThrowingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (DbFailSwitch.Enabled)
            {
                throw new InvalidOperationException("Simulated database persistence failure.");
            }

            return inner.SaveChangesAsync(cancellationToken);
        }
    }
}
