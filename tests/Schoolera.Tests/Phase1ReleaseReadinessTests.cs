using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

/// <summary>
/// Phase 1 release readiness smoke checks: CreateSchool removal, SPA fallbacks, CSRF, seeded fixtures.
/// </summary>
[Collection(WebApplicationFactoryCollection.Name)]
public sealed class Phase1ReleaseReadinessTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public Phase1ReleaseReadinessTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSchool_AnonymousPost_Returns404Or405()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/schools",
            new { nameAr = "مدرسة", nameEn = "School", city = "Cairo" });

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"Expected 404 or 405, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task SpaFallback_ApiUnknown_Returns404NotHtml()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNotHtml(response);
    }

    [Fact]
    public async Task SpaFallback_SwaggerUnknown_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNotHtml(response);
    }

    [Fact]
    public async Task SpaFallback_UploadsMissing_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/uploads/missing-file-phase1-qa.jpg");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNotHtml(response);
    }

    [Fact]
    public async Task Csrf_ContactPost_MissingToken_Returns400()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await client.GetAsync("/api/auth/me");

        var response = await client.PostAsJsonAsync(
            "/api/contact",
            new
            {
                name = "QA Contact",
                phone = "+201011119901",
                category = ContactCategories.General,
                subject = "CSRF check",
                message = "Missing token should fail.",
                consentAccepted = true,
                source = ContactSources.ContactPage,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Csrf_AdminCmsPublish_MissingToken_Returns400()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

        var slug = $"qa-csrf-{Guid.NewGuid():N}"[..18];
        var create = await client.PostAsJsonAsync(
            "/api/admin/cms/pages",
            new
            {
                slug,
                titleAr = "صفحة",
                titleEn = "Page",
                contentAr = "<p>ع</p>",
                contentEn = "<p>A</p>",
            });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var createJson = await create.Content.ReadFromJsonAsync<JsonElement>();
        var pageId = createJson.GetProperty("data").GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var publish = await client.PostAsJsonAsync($"/api/admin/cms/pages/{pageId}/publish", new { });
        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
    }

    [Fact]
    public async Task Csrf_SchoolStartReview_MissingToken_Returns400()
    {
        using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            ownerClient, AuthTestHelpers.SchoolOwnerEmail, AuthTestHelpers.DefaultPassword));

        var schoolId = await GetDemoSchoolIdAsync(ownerClient);
        Assert.NotEqual(Guid.Empty, schoolId);

        // Prefer a real Submitted application when available; otherwise any id still
        // hits antiforgery before the action (authenticated cookie session).
        var applicationId = await FindApplicationIdByStatusAsync(
            ownerClient,
            schoolId,
            AdmissionApplicationStatus.Submitted) ?? Guid.NewGuid();

        ownerClient.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/applications/{applicationId}/start-review",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublicSchool_UnpublishedSlug_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools/demo-unpublished-school");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNotHtml(response);
    }

    [Fact]
    public async Task SchoolPortal_StartReview_AuthorizedOwner_Succeeds()
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

            var submit = await parentClient.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                ownerClient,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword));

            var schoolId = await GetDemoSchoolIdAsync(ownerClient);
            Assert.NotEqual(Guid.Empty, schoolId);

            var start = await ownerClient.PostAsJsonAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{applicationId}/start-review",
                new { internalReviewNote = "Phase1 QA smoke", rowVersion = (byte[]?)null });

            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
            var json = await start.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("succeeded").GetBoolean());
            Assert.Equal(
                (int)AdmissionApplicationStatus.UnderReview,
                json.GetProperty("data").GetProperty("status").GetInt32());
        }
        finally
        {
            var parentClient = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            await AdmissionTestHelpers.RestoreParentCsrfAsync(parentClient);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    private static void AssertNotHtml(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null)
        {
            Assert.DoesNotContain("html", mediaType, StringComparison.OrdinalIgnoreCase);
        }
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

    private static async Task<Guid?> FindApplicationIdByStatusAsync(
        HttpClient client,
        Guid schoolId,
        AdmissionApplicationStatus status)
    {
        var response = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/applications?status={(int)status}&pageNumber=1&pageSize=5");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!json.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("items", out var items))
        {
            return null;
        }

        foreach (var item in items.EnumerateArray())
        {
            return item.GetProperty("id").GetGuid();
        }

        return null;
    }
}
