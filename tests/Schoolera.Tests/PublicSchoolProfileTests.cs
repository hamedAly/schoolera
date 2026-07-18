using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class PublicSchoolProfileTests : IClassFixture<SchooleraWebApplicationFactory>
{
    /// <summary>
    /// Rate limiting is partitioned by IP+slug on a shared test host; serialize contact POSTs
    /// so parallel xUnit cases do not cross-contaminate 429 responses.
    /// </summary>
    private static readonly SemaphoreSlim ContactGate = new(1, 1);

    private readonly SchooleraWebApplicationFactory _factory;

    public PublicSchoolProfileTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublishedProfile_Returns200WithPublicFields()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var data = json.GetProperty("data");
        Assert.Equal(AuthTestHelpers.DemoSchoolSlug, data.GetProperty("slug").GetString());
        Assert.True(data.TryGetProperty("name", out _));
        Assert.True(data.TryGetProperty("additionalServices", out _));
        Assert.True(data.TryGetProperty("branches", out _));
        Assert.True(data.TryGetProperty("fees", out _));
        Assert.True(data.TryGetProperty("isAdmissionOpen", out _));
        Assert.False(data.TryGetProperty("ownerUserId", out _));
        Assert.False(data.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task UnknownSlug_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools/does-not-exist-school-slug");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
        Assert.Contains(
            "school.not_found",
            json.GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task UnpublishedAndSuspended_Return404()
    {
        using var cookieClient = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            cookieClient,
            AuthTestHelpers.PlatformAdminEmail,
            AuthTestHelpers.DefaultPassword);

        var listResponse = await cookieClient.GetAsync("/api/admin/schools?pageNumber=1&pageSize=1&search=cairo");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var schoolId = listJson.GetProperty("data").GetProperty("items")[0].GetProperty("id").GetGuid();
        var slug = listJson.GetProperty("data").GetProperty("items")[0].GetProperty("slug").GetString()!;

        try
        {
            var unpublish = await cookieClient.PostAsJsonAsync(
                $"/api/admin/schools/{schoolId}/status",
                new { status = "Unpublished" });
            Assert.Equal(HttpStatusCode.OK, unpublish.StatusCode);

            using var anon = _factory.CreateClient();
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await anon.GetAsync($"/api/schools/{slug}")).StatusCode);

            var suspend = await cookieClient.PostAsJsonAsync(
                $"/api/admin/schools/{schoolId}/status",
                new { status = "Suspended" });
            Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);

            Assert.Equal(
                HttpStatusCode.NotFound,
                (await anon.GetAsync($"/api/schools/{slug}")).StatusCode);
        }
        finally
        {
            await cookieClient.PostAsJsonAsync(
                $"/api/admin/schools/{schoolId}/status",
                new { status = "Published" });
        }
    }

    [Fact]
    public async Task RelatedSchools_ExcludesCurrent_PublishedOnly_Bounded()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/schools/{AuthTestHelpers.DemoSchoolSlug}/related?limit=4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var items = json.GetProperty("data").EnumerateArray().ToList();
        Assert.True(items.Count <= 4);
        Assert.DoesNotContain(
            items,
            item => item.GetProperty("slug").GetString() == AuthTestHelpers.DemoSchoolSlug);

        var overLimit = await client.GetAsync(
            $"/api/schools/{AuthTestHelpers.DemoSchoolSlug}/related?limit=9");
        Assert.Equal(HttpStatusCode.BadRequest, overLimit.StatusCode);
    }

    [Fact]
    public async Task RelatedUnknownSlug_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools/does-not-exist-school-slug/related");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ContactLead_ValidSubmission_SucceedsWithoutExposingPii()
    {
        await ContactGate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            var me = await client.GetAsync("/api/auth/me");
            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

            var uniquePhone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
            var response = await client.PostAsJsonAsync(
                $"/api/schools/{AuthTestHelpers.DemoSchoolSlug}/contact-leads",
                new
                {
                    name = "Test Parent",
                    phone = uniquePhone,
                    email = "parent-test@example.com",
                    message = "Interested in admissions information.",
                    consentAccepted = true,
                    source = "school-profile",
                    website = (string?)null,
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("succeeded").GetBoolean());
            var data = json.GetProperty("data");
            Assert.True(data.TryGetProperty("leadId", out _));
            Assert.True(data.TryGetProperty("submittedAtUtc", out _));
            Assert.True(data.TryGetProperty("message", out _));
            Assert.False(data.TryGetProperty("phone", out _));
            Assert.False(data.TryGetProperty("email", out _));
            Assert.False(data.TryGetProperty("name", out _));
        }
        finally
        {
            ContactGate.Release();
        }
    }

    [Fact]
    public async Task ContactLead_ConsentRequired_InvalidSource_Honeypot_InvalidEmail()
    {
        await ContactGate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            var me = await client.GetAsync("/api/auth/me");
            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

            // Validation probes use the 2nd published school; rate-limit burst uses the 3rd.
            var slug = await ResolveNthPublishedSlugAsync(1) ?? AuthTestHelpers.DemoSchoolSlug;

            var noConsent = await client.PostAsJsonAsync(
                $"/api/schools/{slug}/contact-leads",
                new
                {
                    name = "A",
                    phone = "+201011111111",
                    consentAccepted = false,
                    source = "school-profile",
                });
            Assert.Equal(HttpStatusCode.BadRequest, noConsent.StatusCode);
            var noConsentJson = await noConsent.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(
                "school.contact.consent_required",
                noConsentJson.GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString()));

            var badSource = await client.PostAsJsonAsync(
                $"/api/schools/{slug}/contact-leads",
                new
                {
                    name = "A",
                    phone = "+201011111112",
                    consentAccepted = true,
                    source = "evil-tracker",
                });
            Assert.Equal(HttpStatusCode.BadRequest, badSource.StatusCode);

            var honeypot = await client.PostAsJsonAsync(
                $"/api/schools/{slug}/contact-leads",
                new
                {
                    name = "A",
                    phone = "+201011111113",
                    consentAccepted = true,
                    source = "school-profile",
                    website = "http://spam.example",
                });
            Assert.Equal(HttpStatusCode.BadRequest, honeypot.StatusCode);

            var badEmail = await client.PostAsJsonAsync(
                $"/api/schools/{slug}/contact-leads",
                new
                {
                    name = "A",
                    phone = "+201011111114",
                    email = "not-an-email",
                    consentAccepted = true,
                    source = "school-profile",
                });
            Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
        }
        finally
        {
            ContactGate.Release();
        }
    }

    [Fact]
    public async Task ContactLead_UnpublishedSchool_Returns404()
    {
        await ContactGate.WaitAsync();
        try
        {
            using var cookieClient = AuthTestHelpers.CreateCookieClient(_factory);
            await AuthTestHelpers.TryLoginAsync(
                cookieClient,
                AuthTestHelpers.PlatformAdminEmail,
                AuthTestHelpers.DefaultPassword);

            var listResponse = await cookieClient.GetAsync("/api/admin/schools?pageNumber=1&pageSize=1");
            var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
            var schoolId = listJson.GetProperty("data").GetProperty("items")[0].GetProperty("id").GetGuid();
            var slug = listJson.GetProperty("data").GetProperty("items")[0].GetProperty("slug").GetString()!;

            try
            {
                await cookieClient.PostAsJsonAsync(
                    $"/api/admin/schools/{schoolId}/status",
                    new { status = "Unpublished" });

                var me = await cookieClient.GetAsync("/api/auth/me");
                AuthTestHelpers.ApplyAntiforgeryFromResponse(cookieClient, me);

                var response = await cookieClient.PostAsJsonAsync(
                    $"/api/schools/{slug}/contact-leads",
                    new
                    {
                        name = "A",
                        phone = "+201022222222",
                        consentAccepted = true,
                        source = "school-profile",
                    });

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
            finally
            {
                await cookieClient.PostAsJsonAsync(
                    $"/api/admin/schools/{schoolId}/status",
                    new { status = "Published" });
            }
        }
        finally
        {
            ContactGate.Release();
        }
    }

    [Fact]
    public async Task ContactLead_RateLimited_AfterBurst()
    {
        await ContactGate.WaitAsync();
        try
        {
            var slug = await ResolveNthPublishedSlugAsync(2)
                ?? await ResolveNthPublishedSlugAsync(1)
                ?? AuthTestHelpers.DemoSchoolSlug;

            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            var me = await client.GetAsync("/api/auth/me");
            AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

            HttpStatusCode? last = null;
            for (var i = 0; i < 25; i++)
            {
                var response = await client.PostAsJsonAsync(
                    $"/api/schools/{slug}/contact-leads",
                    new
                    {
                        name = "Rate Limit Parent",
                        phone = $"+20103{i:0000000}",
                        consentAccepted = true,
                        source = "school-profile",
                    });
                last = response.StatusCode;
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    break;
                }
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, last);
        }
        finally
        {
            ContactGate.Release();
        }
    }

    [Fact]
    public async Task ProfileView_DoesNotFailResponse_AndAggregatesDaily()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Allow background worker a short window to upsert.
        await Task.Delay(1500);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var row = db.SchoolProfileViewDaily
            .FirstOrDefault(view => view.ViewDateUtc == today);
        Assert.NotNull(row);
        Assert.True(row!.ViewCount >= 1);
    }

    private async Task<string?> ResolveNthPublishedSlugAsync(int skipDemoRelativeIndex)
    {
        using var listClient = _factory.CreateClient();
        var listResponse = await listClient.GetAsync("/api/schools?pageNumber=1&pageSize=20&sort=newest");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var others = listJson.GetProperty("data").GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("slug").GetString()!)
            .Where(slug => slug != AuthTestHelpers.DemoSchoolSlug)
            .ToArray();

        return skipDemoRelativeIndex >= 0 && skipDemoRelativeIndex < others.Length
            ? others[skipDemoRelativeIndex]
            : null;
    }
}
