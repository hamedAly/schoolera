using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class SchoolAdmissionReviewTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public SchoolAdmissionReviewTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SchoolApplications_Anonymous_Returns401()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        var response = await client.GetAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/applications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SchoolApplications_WhenParent_Returns403()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.ParentEmail,
            AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/applications");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolApplications_WhenPlatformAdmin_Returns403()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.PlatformAdminEmail,
            AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/applications");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolApplications_WhenSupportAgent_Returns403()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SupportAgentEmail,
            AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/applications");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolApplications_WhenSchoolAdmin_WithMembership_Allowed()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolAdminEmail,
            AuthTestHelpers.DefaultPassword));

        var schoolId = await GetDemoSchoolIdAsync(client);
        Assert.NotEqual(Guid.Empty, schoolId);

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/applications?pageNumber=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task SchoolApplications_WhenOtherSchool_Returns404()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/applications");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(SchoolPortalErrorCodes.SchoolNotFound, ReadCodes(json));
    }

    [Fact]
    public async Task ReviewFlow_StartAcceptReject_AndInvalidTransitions()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

            var parentClient = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, parentClient);
            var childAccept = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);
            var childReject = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);

            var draftAccept = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient,
                context,
                childAccept,
                context.Slots[0]);
            var draftReject = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient,
                context,
                childReject,
                context.Slots[0]);

            var acceptId = draftAccept.GetProperty("id").GetGuid();
            var rejectId = draftReject.GetProperty("id").GetGuid();

            await SubmitDraftAsync(parentClient, acceptId);
            await SubmitDraftAsync(parentClient, rejectId);

            using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                ownerClient,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword));

            var schoolId = await GetDemoSchoolIdAsync(ownerClient);
            Assert.NotEqual(Guid.Empty, schoolId);

            // Submitted → Accepted directly must fail
            var directAccept = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{acceptId}/accept",
                new { internalReviewNote = (string?)null, rowVersion = (byte[]?)null });
            Assert.Equal(HttpStatusCode.BadRequest, directAccept.StatusCode);
            var directAcceptJson = await directAccept.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(AdmissionErrorCodes.ReviewInvalidTransition, ReadCodes(directAcceptJson));

            var startAccept = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{acceptId}/start-review",
                new { internalReviewNote = "Starting", rowVersion = (byte[]?)null });
            Assert.Equal(HttpStatusCode.OK, startAccept.StatusCode);
            var startAcceptJson = await startAccept.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(startAcceptJson.GetProperty("succeeded").GetBoolean());
            Assert.Equal(
                (int)AdmissionApplicationStatus.UnderReview,
                startAcceptJson.GetProperty("data").GetProperty("status").GetInt32());

            var accept = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{acceptId}/accept",
                new { internalReviewNote = "Accepted internally", rowVersion = (byte[]?)null });
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
            var acceptJson = await accept.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                (int)AdmissionApplicationStatus.Accepted,
                acceptJson.GetProperty("data").GetProperty("status").GetInt32());
            Assert.Equal(
                "Accepted internally",
                acceptJson.GetProperty("data").GetProperty("schoolNotes").GetString());

            var startReject = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{rejectId}/start-review",
                new { });
            Assert.Equal(HttpStatusCode.OK, startReject.StatusCode);

            var rejectMissingReason = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{rejectId}/reject",
                new { parentVisibleRejectionReason = "", internalReviewNote = "hidden" });
            Assert.Equal(HttpStatusCode.BadRequest, rejectMissingReason.StatusCode);

            var reject = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{rejectId}/reject",
                new
                {
                    parentVisibleRejectionReason = "Incomplete documents",
                    internalReviewNote = "Internal only",
                });
            Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

            // Parent sees rejection reason, not internal note
            var parentDetail = await parentClient.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{rejectId}");
            Assert.Equal(HttpStatusCode.OK, parentDetail.StatusCode);
            var parentJson = await parentDetail.Content.ReadFromJsonAsync<JsonElement>();
            var parentData = parentJson.GetProperty("data");
            Assert.Equal(
                "Incomplete documents",
                parentData.GetProperty("parentVisibleRejectionReason").GetString());
            Assert.False(parentData.TryGetProperty("schoolNotes", out _));

            // Dashboard admissions available for school
            var dash = await ownerClient.GetAsync($"/api/school-portal/schools/{schoolId}/dashboard");
            Assert.Equal(HttpStatusCode.OK, dash.StatusCode);
            var dashJson = await dash.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(dashJson.GetProperty("data").GetProperty("admissionsAvailable").GetBoolean());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task SchoolList_FiltersByStatus_AndDetailMasksIdentity()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

            var parentClient = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, parentClient);
            var childId = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();
            await SubmitDraftAsync(parentClient, applicationId);

            using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                ownerClient,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword));
            var schoolId = await GetDemoSchoolIdAsync(ownerClient);

            var list = await ownerClient.GetAsync(
                $"/api/school-portal/schools/{schoolId}/applications?status={(int)AdmissionApplicationStatus.Submitted}&pageNumber=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var listData = (await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            var items = listData.GetProperty("items").EnumerateArray().ToList();
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == applicationId);
            Assert.All(
                items,
                item => Assert.Equal(
                    (int)AdmissionApplicationStatus.Submitted,
                    item.GetProperty("status").GetInt32()));

            var detail = await ownerClient.GetAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{applicationId}");
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var data = (await detail.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.True(data.TryGetProperty("studentMaskedIdentity", out _));
            Assert.False(data.TryGetProperty("storageKey", out _));
            Assert.False(data.TryGetProperty("identityHash", out _));
            Assert.False(data.TryGetProperty("identityCiphertext", out _));

            // Cross-school detail
            var otherDetail = await ownerClient.GetAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, otherDetail.StatusCode);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task SchoolAttachmentDownload_Authorized_AndOtherSchoolDenied()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

            var parentClient = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, parentClient);
            var childId = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            Guid attachmentId;
            using (var uploadContent = AdmissionTestHelpers.BuildPdfUploadContent(
                       AdmissionTestHelpers.MinimalPdfBytes))
            {
                var upload = await parentClient.PostAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                    uploadContent);
                Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
                var uploaded = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
                attachmentId = uploaded.GetProperty("attachments").EnumerateArray().First()
                    .GetProperty("id").GetGuid();
            }

            await SubmitDraftAsync(parentClient, applicationId);

            using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                ownerClient,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword));
            var schoolId = await GetDemoSchoolIdAsync(ownerClient);

            var download = await ownerClient.GetAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{applicationId}/attachments/{attachmentId}/download");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
            var bytes = await download.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length >= 5);
            Assert.Equal((byte)'%', bytes[0]);

            var wrongSchool = await ownerClient.GetAsync(
                $"/api/school-portal/schools/{Guid.NewGuid()}/applications/{applicationId}/attachments/{attachmentId}/download");
            Assert.Equal(HttpStatusCode.NotFound, wrongSchool.StatusCode);

            var missing = await ownerClient.GetAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{applicationId}/attachments/{Guid.NewGuid()}/download");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Review_SubmittedToRejectDirect_AndCancelledBlocked()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

            var parentClient = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, parentClient);
            var childSubmit = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);
            var childCancel = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);

            var draftSubmit = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient, context, childSubmit, context.Slots[0]);
            var draftCancel = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient, context, childCancel, context.Slots[0]);
            var submitId = draftSubmit.GetProperty("id").GetGuid();
            var cancelId = draftCancel.GetProperty("id").GetGuid();

            await SubmitDraftAsync(parentClient, submitId);
            var cancel = await parentClient.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{cancelId}/cancel",
                new { reason = "parent cancelled" });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

            using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                ownerClient,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword));
            var schoolId = await GetDemoSchoolIdAsync(ownerClient);

            var directReject = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{submitId}/reject",
                new
                {
                    parentVisibleRejectionReason = "Should fail",
                    internalReviewNote = (string?)null,
                });
            Assert.Equal(HttpStatusCode.BadRequest, directReject.StatusCode);
            Assert.Contains(
                AdmissionErrorCodes.ReviewInvalidTransition,
                ReadCodes(await directReject.Content.ReadFromJsonAsync<JsonElement>()));

            var reviewCancelled = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{cancelId}/start-review",
                new { });
            Assert.Equal(HttpStatusCode.BadRequest, reviewCancelled.StatusCode);
            Assert.Contains(
                AdmissionErrorCodes.ReviewInvalidTransition,
                ReadCodes(await reviewCancelled.Content.ReadFromJsonAsync<JsonElement>()));
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task AdminMonitoring_AnonymousAndParentDenied()
    {
        using var anon = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anon.GetAsync("/api/admin/admission-applications")).StatusCode);

        using var parent = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            parent,
            AuthTestHelpers.ParentEmail,
            AuthTestHelpers.DefaultPassword));
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await parent.GetAsync("/api/admin/admission-applications")).StatusCode);
    }

    [Fact]
    public async Task AdminMonitoring_ListExport_AndReadOnly()
    {
        using var adminClient = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            adminClient,
            AuthTestHelpers.PlatformAdminEmail,
            AuthTestHelpers.DefaultPassword));

        var list = await adminClient.GetAsync("/api/admin/admission-applications?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listJson = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(listJson.GetProperty("succeeded").GetBoolean());

        var items = listJson.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        if (items.Count > 0)
        {
            var applicationId = items[0].GetProperty("id").GetGuid();
            var detail = await adminClient.GetAsync($"/api/admin/admission-applications/{applicationId}");
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var detailData = (await detail.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.True(detailData.TryGetProperty("studentMaskedIdentity", out _));
            Assert.False(detailData.TryGetProperty("storageKey", out _));
            Assert.False(detailData.TryGetProperty("identityHash", out _));
        }

        var export = await adminClient.GetAsync("/api/admin/admission-applications/export");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Contains("text/csv", export.Content.Headers.ContentType?.MediaType ?? string.Empty);
        var csv = await export.Content.ReadAsStringAsync();
        Assert.Contains("ApplicationNumber", csv);
        Assert.DoesNotContain("Identity", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StorageKey", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SchoolNotes", csv, StringComparison.OrdinalIgnoreCase);

        var dash = await adminClient.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, dash.StatusCode);
        var dashJson = await dash.Content.ReadFromJsonAsync<JsonElement>();
        var dashData = dashJson.GetProperty("data");
        Assert.True(dashData.GetProperty("admissionsAvailable").GetBoolean());
        Assert.True(dashData.TryGetProperty("admissionApplicationsCount", out var appsCount)
            && appsCount.ValueKind == JsonValueKind.Number);
        Assert.True(dashData.TryGetProperty("admissionPendingSchoolReviewCount", out var pending)
            && pending.ValueKind == JsonValueKind.Number);

        // No admin state-changing endpoints
        var fakeAccept = await adminClient.PostAsJsonAsync(
            $"/api/admin/admission-applications/{Guid.NewGuid()}/accept",
            new { });
        Assert.Equal(HttpStatusCode.NotFound, fakeAccept.StatusCode);
    }

    [Fact]
    public async Task AdminCsvExporter_EscapesFormulaInjection()
    {
        var (bytes, _) = Schoolera.Application.Admissions.Common.AdminAdmissionCsvExporter.Build(
            [
                new Schoolera.Application.Admissions.Dtos.AdminAdmissionExportRowDto(
                    "APP-2026-000001",
                    "Submitted",
                    "School",
                    "Cairo",
                    "Main",
                    "=cmd",
                    "+Parent",
                    "G1",
                    "2026/2027",
                    "2026-01-01T00:00:00Z",
                    "",
                    "",
                    []),
            ]);

        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("'=cmd", text);
        Assert.Contains("'+Parent", text);
        Assert.StartsWith("\uFEFF", text);
    }

    private static async Task SubmitDraftAsync(HttpClient client, Guid applicationId)
    {
        var response = await client.PostAsJsonAsync(
            $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
            new { termsAccepted = true, privacyAccepted = true });
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Submit failed: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task<Guid> GetDemoSchoolIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/school-portal/schools");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in json.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("slug").GetString() == AuthTestHelpers.DemoSchoolSlug)
            {
                return item.GetProperty("id").GetGuid();
            }
        }

        return Guid.Empty;
    }

    private static string[] ReadCodes(JsonElement json) =>
        json.TryGetProperty("errorCodes", out var codes)
            ? codes.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray()
            : [];
}
