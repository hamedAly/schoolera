using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Schoolera.Application.Cms.Constants;
using Schoolera.Infrastructure.Cms;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class CmsAndContactTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private static readonly SemaphoreSlim ContactGate = new(1, 1);
    private readonly SchooleraWebApplicationFactory _factory;

    public CmsAndContactTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicPage_PublishedSlug_ReturnsLocalizedContent()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/content/pages/about");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var data = json.GetProperty("data");
        Assert.Equal("about", data.GetProperty("slug").GetString());
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("content").GetString()));
    }

    [Fact]
    public async Task PublicPage_UnknownSlug_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/content/pages/does-not-exist-page");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            CmsErrorCodes.PageNotFound,
            json.GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task PublicFaqs_ReturnsPublishedCategoriesOnly()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/content/faqs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var categories = json.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(categories);
        Assert.All(categories, category => Assert.NotEmpty(category.GetProperty("items").EnumerateArray()));
    }

    [Fact]
    public async Task PublicHome_ReturnsPublishedHomepage()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/content/home");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("data").GetProperty("heroTitle").GetString()));
    }

    [Fact]
    public void ContentSanitizer_StripsScriptAndEventHandlers()
    {
        var sanitizer = new ContentSanitizer();
        var dirty = "<p onclick=\"alert(1)\">Hello</p><script>alert(1)</script><h2>Title</h2>";
        var clean = sanitizer.SanitizeHtml(dirty);

        Assert.DoesNotContain("script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello", clean, StringComparison.Ordinal);
        Assert.Contains("Title", clean, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContactSubmit_ValidRequest_ReturnsReferenceOnly()
    {
        await ContactGate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            var me = await client.GetAsync("/api/auth/me");
            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

            var uniquePhone = $"+2015{Random.Shared.Next(10000000, 99999999)}";
            var response = await client.PostAsJsonAsync(
                "/api/contact",
                new
                {
                    name = "Test User",
                    phone = uniquePhone,
                    email = "cms-test@example.com",
                    category = ContactCategories.General,
                    subject = $"CMS test {Guid.NewGuid():N}",
                    message = "Need help with Schoolera.",
                    consentAccepted = true,
                    source = ContactSources.ContactPage,
                    website = (string?)null,
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("succeeded").GetBoolean());
            var data = json.GetProperty("data");
            Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("reference").GetString()));
            Assert.False(data.TryGetProperty("name", out _));
            Assert.False(data.TryGetProperty("phone", out _));
            Assert.False(data.TryGetProperty("email", out _));
            Assert.False(data.TryGetProperty("message", out _));
        }
        finally
        {
            ContactGate.Release();
        }
    }

    [Fact]
    public async Task ContactSubmit_ConsentRequired_Honeypot_InvalidCategory()
    {
        await ContactGate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            var me = await client.GetAsync("/api/auth/me");
            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

            var noConsent = await client.PostAsJsonAsync(
                "/api/contact",
                new
                {
                    name = "A",
                    phone = "+201011111201",
                    category = ContactCategories.General,
                    subject = "No consent",
                    message = "Test",
                    consentAccepted = false,
                    source = ContactSources.ContactPage,
                });
            Assert.Equal(HttpStatusCode.BadRequest, noConsent.StatusCode);

            var honeypot = await client.PostAsJsonAsync(
                "/api/contact",
                new
                {
                    name = "A",
                    phone = "+201011111202",
                    category = ContactCategories.General,
                    subject = "Honeypot",
                    message = "Test",
                    consentAccepted = true,
                    source = ContactSources.ContactPage,
                    website = "https://spam.example",
                });
            Assert.Equal(HttpStatusCode.BadRequest, honeypot.StatusCode);

            var badCategory = await client.PostAsJsonAsync(
                "/api/contact",
                new
                {
                    name = "A",
                    phone = "+201011111203",
                    category = "invalid-category",
                    subject = "Bad category",
                    message = "Test",
                    consentAccepted = true,
                    source = ContactSources.ContactPage,
                });
            Assert.Equal(HttpStatusCode.BadRequest, badCategory.StatusCode);
        }
        finally
        {
            ContactGate.Release();
        }
    }

    [Fact]
    public async Task ContactSubmit_MissingCsrf_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await client.GetAsync("/api/auth/me");

        var response = await client.PostAsJsonAsync(
            "/api/contact",
            new
            {
                name = "A",
                phone = "+201011111204",
                category = ContactCategories.General,
                subject = "No CSRF",
                message = "Test",
                consentAccepted = true,
                source = ContactSources.ContactPage,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/cms/pages")]
    [InlineData("/api/admin/cms/faq/categories")]
    [InlineData("/api/admin/cms/home")]
    [InlineData("/api/admin/contact-requests")]
    public async Task AdminCmsEndpoints_WhenAnonymous_Return401(string path)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCmsPages_WhenParent_Returns403()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync("/api/admin/cms/pages");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminCms_CreatePage_RejectsReservedAndInvalidSlugs()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));
        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

        var reserved = await client.PostAsJsonAsync(
            "/api/admin/cms/pages",
            new
            {
                slug = "admin",
                titleAr = "ع",
                titleEn = "A",
                contentAr = "<p>ع</p>",
                contentEn = "<p>A</p>",
            });
        Assert.Equal(HttpStatusCode.BadRequest, reserved.StatusCode);

        var invalid = await client.PostAsJsonAsync(
            "/api/admin/cms/pages",
            new
            {
                slug = "Bad Slug!",
                titleAr = "ع",
                titleEn = "A",
                contentAr = "<p>ع</p>",
                contentEn = "<p>A</p>",
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task AdminCms_CreatePage_SanitizesScriptOnSave()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));
        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

        var slug = $"cms-test-{Guid.NewGuid():N}"[..20];
        var create = await client.PostAsJsonAsync(
            "/api/admin/cms/pages",
            new
            {
                slug,
                titleAr = "صفحة",
                titleEn = "Page",
                contentAr = "<p>آمن</p><script>alert(1)</script>",
                contentEn = "<p>Safe</p><script>alert(1)</script>",
            });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        var json = await create.Content.ReadFromJsonAsync<JsonElement>();
        var contentEn = json.GetProperty("data").GetProperty("contentEn").GetString();
        Assert.DoesNotContain("script", contentEn, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminCms_FaqReorder_RejectsInvalidIds()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));
        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

        var response = await client.PostAsJsonAsync(
            "/api/admin/cms/faq/categories/reorder",
            new { orderedIds = new[] { Guid.NewGuid(), Guid.NewGuid() } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            CmsErrorCodes.FaqReorderInvalid,
            json.GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task AdminCms_HomepageUpdate_RejectsInvalidCtaUrl()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

        var get = await client.GetAsync("/api/admin/cms/home");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var current = await get.Content.ReadFromJsonAsync<JsonElement>();
        var data = current.GetProperty("data");

        var update = await client.PutAsJsonAsync(
            "/api/admin/cms/home",
            new
            {
                heroTitleAr = data.GetProperty("heroTitleAr").GetString(),
                heroTitleEn = data.GetProperty("heroTitleEn").GetString(),
                heroSubtitleAr = data.GetProperty("heroSubtitleAr").GetString(),
                heroSubtitleEn = data.GetProperty("heroSubtitleEn").GetString(),
                primaryCtaLabelAr = data.GetProperty("primaryCtaLabelAr").GetString(),
                primaryCtaLabelEn = data.GetProperty("primaryCtaLabelEn").GetString(),
                primaryCtaUrl = "javascript:alert(1)",
                secondaryCtaLabelAr = data.TryGetProperty("secondaryCtaLabelAr", out var sAr) && sAr.ValueKind != JsonValueKind.Null
                    ? sAr.GetString()
                    : null,
                secondaryCtaLabelEn = data.TryGetProperty("secondaryCtaLabelEn", out var sEn) && sEn.ValueKind != JsonValueKind.Null
                    ? sEn.GetString()
                    : null,
                secondaryCtaUrl = data.TryGetProperty("secondaryCtaUrl", out var sUrl) && sUrl.ValueKind != JsonValueKind.Null
                    ? sUrl.GetString()
                    : null,
                schoolsSectionTitleAr = data.GetProperty("schoolsSectionTitleAr").GetString(),
                schoolsSectionTitleEn = data.GetProperty("schoolsSectionTitleEn").GetString(),
                parentJourneyTitleAr = data.GetProperty("parentJourneyTitleAr").GetString(),
                parentJourneyTitleEn = data.GetProperty("parentJourneyTitleEn").GetString(),
                parentJourneyTextAr = data.GetProperty("parentJourneyTextAr").GetString(),
                parentJourneyTextEn = data.GetProperty("parentJourneyTextEn").GetString(),
                schoolJourneyTitleAr = data.GetProperty("schoolJourneyTitleAr").GetString(),
                schoolJourneyTitleEn = data.GetProperty("schoolJourneyTitleEn").GetString(),
                schoolJourneyTextAr = data.GetProperty("schoolJourneyTextAr").GetString(),
                schoolJourneyTextEn = data.GetProperty("schoolJourneyTextEn").GetString(),
                faqSectionTitleAr = data.GetProperty("faqSectionTitleAr").GetString(),
                faqSectionTitleEn = data.GetProperty("faqSectionTitleEn").GetString(),
                faqSectionSubtitleAr = data.TryGetProperty("faqSectionSubtitleAr", out var fAr) && fAr.ValueKind != JsonValueKind.Null
                    ? fAr.GetString()
                    : null,
                faqSectionSubtitleEn = data.TryGetProperty("faqSectionSubtitleEn", out var fEn) && fEn.ValueKind != JsonValueKind.Null
                    ? fEn.GetString()
                    : null,
            });

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        var updateJson = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            CmsErrorCodes.HomeInvalidCtaUrl,
            updateJson.GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task AdminContact_ListExcludesMessageBody_DetailIncludesMessage()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        var list = await client.GetAsync("/api/admin/contact-requests");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listJson = await list.Content.ReadFromJsonAsync<JsonElement>();
        var items = listJson.GetProperty("data").GetProperty("items").EnumerateArray().ToArray();
        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.False(item.TryGetProperty("message", out _)));

        var id = items[0].GetProperty("id").GetGuid();
        var detail = await client.GetAsync($"/api/admin/contact-requests/{id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailJson = await detail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(detailJson.GetProperty("data").TryGetProperty("message", out _));
    }

    [Fact]
    public async Task AdminContact_StatusTransitions_WriteAuditEvents()
    {
        await ContactGate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

            var me = await client.GetAsync("/api/auth/me");
            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

            var uniquePhone = $"+2016{Random.Shared.Next(10000000, 99999999)}";
            var submit = await client.PostAsJsonAsync(
                "/api/contact",
                new
                {
                    name = "Audit User",
                    phone = uniquePhone,
                    category = ContactCategories.General,
                    subject = $"Audit flow {Guid.NewGuid():N}",
                    message = "Audit transition test.",
                    consentAccepted = true,
                    source = ContactSources.Help,
                });
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);
            var list = await client.GetAsync("/api/admin/contact-requests?search=Audit flow");
            var listJson = await list.Content.ReadFromJsonAsync<JsonElement>();
            var item = listJson.GetProperty("data").GetProperty("items").EnumerateArray().First();
            var id = item.GetProperty("id").GetGuid();

            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);
            var review = await client.PostAsJsonAsync(
                $"/api/admin/contact-requests/{id}/start-review",
                new { adminNote = "Review started" });
            Assert.Equal(HttpStatusCode.OK, review.StatusCode);

            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);
            var resolve = await client.PostAsJsonAsync(
                $"/api/admin/contact-requests/{id}/resolve",
                new { adminNote = "Resolved" });
            Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);

            var audit = await client.GetAsync("/api/admin/audit?action=contact.status_changed");
            Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
            var auditJson = await audit.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(auditJson.GetProperty("data").GetProperty("totalCount").GetInt32() >= 1);
        }
        finally
        {
            ContactGate.Release();
        }
    }
}
