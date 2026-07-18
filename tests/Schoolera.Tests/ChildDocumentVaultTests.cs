using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class ChildDocumentVaultTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static readonly byte[] MinimalPdfBytes = AdmissionTestHelpers.MinimalPdfBytes;
    private static readonly byte[] MinimalPngBytes =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00];

    private readonly SchooleraWebApplicationFactory _factory;

    public ChildDocumentVaultTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_List_Download_Replace_Delete_RoundTrip()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var childId = await CreateChildAsync(client);

            using (var upload = BuildUploadContent(MinimalPdfBytes, ChildDocumentType.BirthCertificate, "birth.pdf", "application/pdf"))
            {
                var response = await client.PostAsync($"/api/parent/children/{childId}/documents", upload);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var created = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
                Assert.False(created.TryGetProperty("storageKey", out _));
                Assert.Equal((int)ChildDocumentType.BirthCertificate, created.GetProperty("documentType").GetInt32());
                var documentId = created.GetProperty("id").GetGuid();

                var list = await client.GetAsync($"/api/parent/children/{childId}/documents");
                list.EnsureSuccessStatusCode();
                var items = (await list.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("data").EnumerateArray().ToList();
                Assert.Contains(items, item => item.GetProperty("id").GetGuid() == documentId);
                Assert.All(items, item => Assert.False(item.TryGetProperty("storageKey", out _)));

                var download = await client.GetAsync(
                    $"/api/parent/children/{childId}/documents/{documentId}/download");
                Assert.Equal(HttpStatusCode.OK, download.StatusCode);
                Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
                Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
                Assert.Contains("no-store", download.Headers.CacheControl?.ToString() ?? string.Empty);

                using (var replace = BuildUploadContent(
                           MinimalPngBytes,
                           ChildDocumentType.ChildPhoto,
                           "photo.png",
                           "image/png"))
                {
                    var replaced = await client.PutAsync(
                        $"/api/parent/children/{childId}/documents/{documentId}",
                        replace);
                    Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
                    var replacedData = (await replaced.Content.ReadFromJsonAsync<JsonElement>())
                        .GetProperty("data");
                    Assert.Equal((int)ChildDocumentType.ChildPhoto, replacedData.GetProperty("documentType").GetInt32());
                    Assert.Equal("image/png", replacedData.GetProperty("contentType").GetString());
                }

                var delete = await client.DeleteAsync(
                    $"/api/parent/children/{childId}/documents/{documentId}");
                Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

                var missing = await client.GetAsync(
                    $"/api/parent/children/{childId}/documents/{documentId}/download");
                Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
                var codes = ReadErrorCodes(await missing.Content.ReadFromJsonAsync<JsonElement>());
                Assert.Contains("parent.child.documentNotFound", codes);
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task OwnershipIsolation_UnknownDocument_ReturnsSame404()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var childId = await CreateChildAsync(client);
            var unknown = await client.GetAsync(
                $"/api/parent/children/{childId}/documents/{Guid.NewGuid()}/download");
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Contains(
                "parent.child.documentNotFound",
                ReadErrorCodes(await unknown.Content.ReadFromJsonAsync<JsonElement>()));
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task WrongRole_Returns403()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword);
        var response = await client.GetAsync($"/api/parent/children/{Guid.NewGuid()}/documents");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolPortal_CannotAccessVaultEndpoints()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolAdminEmail,
            AuthTestHelpers.DefaultPassword);
        var response = await client.GetAsync($"/api/parent/children/{Guid.NewGuid()}/documents");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidPhotoType_AndInvalidFile_Rejected()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var childId = await CreateChildAsync(client);

            using (var pdfAsPhoto = BuildUploadContent(
                       MinimalPdfBytes,
                       ChildDocumentType.ChildPhoto,
                       "not-photo.pdf",
                       "application/pdf"))
            {
                var response = await client.PostAsync(
                    $"/api/parent/children/{childId}/documents",
                    pdfAsPhoto);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains(
                    "parent.child.documentInvalidFile",
                    ReadErrorCodes(await response.Content.ReadFromJsonAsync<JsonElement>()));
            }

            using (var garbage = BuildUploadContent(
                       Encoding.UTF8.GetBytes("not a real file"),
                       ChildDocumentType.BirthCertificate,
                       "bad.pdf",
                       "application/pdf"))
            {
                var response = await client.PostAsync(
                    $"/api/parent/children/{childId}/documents",
                    garbage);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains(
                    "parent.child.documentInvalidFile",
                    ReadErrorCodes(await response.Content.ReadFromJsonAsync<JsonElement>()));
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task Upload_MissingCsrf_Returns400()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var childId = await CreateChildAsync(client);
            client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

            using var upload = BuildUploadContent(
                MinimalPdfBytes,
                ChildDocumentType.BirthCertificate,
                "birth.pdf",
                "application/pdf");
            var response = await client.PostAsync($"/api/parent/children/{childId}/documents", upload);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task CopyToDraft_IsIdempotent_AndSurvivesVaultDeleteAfterSubmit()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            Guid documentId;
            using (var upload = BuildUploadContent(
                       AdmissionTestHelpers.MinimalPdfBytes,
                       ChildDocumentType.BirthCertificate,
                       "birth.pdf",
                       "application/pdf"))
            {
                var uploaded = await client.PostAsync(
                    $"/api/parent/children/{childId}/documents",
                    upload);
                uploaded.EnsureSuccessStatusCode();
                documentId = (await uploaded.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("data").GetProperty("id").GetGuid();
            }

            var copy1 = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments/from-vault",
                new { childDocumentId = documentId });
            Assert.Equal(HttpStatusCode.OK, copy1.StatusCode);
            var detail1 = (await copy1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            var attachments1 = detail1.GetProperty("attachments").EnumerateArray().ToList();
            Assert.Single(attachments1);
            Assert.All(attachments1, item => Assert.False(item.TryGetProperty("storageKey", out _)));
            Assert.All(attachments1, item => Assert.False(item.TryGetProperty("sourceVaultDocumentId", out _)));
            var attachmentId = attachments1[0].GetProperty("id").GetGuid();

            var copy2 = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments/from-vault",
                new { childDocumentId = documentId });
            Assert.Equal(HttpStatusCode.OK, copy2.StatusCode);
            var attachments2 = (await copy2.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").GetProperty("attachments").EnumerateArray().ToList();
            Assert.Single(attachments2);
            Assert.Equal(attachmentId, attachments2[0].GetProperty("id").GetGuid());

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            submit.EnsureSuccessStatusCode();

            var deleteVault = await client.DeleteAsync(
                $"/api/parent/children/{childId}/documents/{documentId}");
            Assert.Equal(HttpStatusCode.OK, deleteVault.StatusCode);

            var downloadAttachment = await client.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments/{attachmentId}/download");
            Assert.Equal(HttpStatusCode.OK, downloadAttachment.StatusCode);
            Assert.Equal("application/pdf", downloadAttachment.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    private async Task<HttpClient> CreateParentClientAsync()
    {
        var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AdmissionTestHelpers.LoginWithRetryAsync(
            client,
            AuthTestHelpers.ParentEmail,
            AuthTestHelpers.DefaultPassword);
        return client;
    }

    private static async Task<Guid> CreateChildAsync(HttpClient client, Guid? gradeId = null)
    {
        var resolvedGradeId = gradeId ?? await ResolveActiveGradeIdAsync(client);
        var create = await client.PostAsJsonAsync(
            "/api/parent/children",
            new
            {
                fullName = $"Vault Child {Random.Shared.Next(1000, 9999)}",
                identityType = 1,
                identityValue = $"2880101{Random.Shared.Next(100000, 999999)}",
                birthDate = "2015-05-01",
                gender = 1,
                currentGradeId = resolvedGradeId,
                hasSpecialNeeds = false,
                specialNeedsNotes = (string?)null,
                currentSchoolName = "Prior School",
                preferredStudyLanguage = (int)ChildStudyLanguage.English,
                skills = "Drawing",
                hobbies = "Football",
                strengths = "Focus",
                improvementAreas = "Math",
                healthNotes = "Parent-only note",
            });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        return (await create.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> ResolveActiveGradeIdAsync(HttpClient client)
    {
        var stages = await client.GetAsync("/api/taxonomies/educational-stages");
        stages.EnsureSuccessStatusCode();
        var stageId = (await stages.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();
        var grades = await client.GetAsync($"/api/taxonomies/educational-stages/{stageId}/grades");
        grades.EnsureSuccessStatusCode();
        return (await grades.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent BuildUploadContent(
        byte[] bytes,
        ChildDocumentType documentType,
        string fileName,
        string contentType)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        content.Add(new StringContent(((int)documentType).ToString()), "documentType");
        return content;
    }

    private static IEnumerable<string?> ReadErrorCodes(JsonElement json) =>
        json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString());
}
