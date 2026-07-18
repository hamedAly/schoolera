using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Schoolera.Application.SchoolPortal.Constants;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class SchoolPortalIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public SchoolPortalIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListSchools_WhenSchoolOwner_ReturnsAccessibleDemoSchool()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync("/api/school-portal/schools");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());

        var items = json.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(items);
        Assert.Contains(
            items,
            item => item.GetProperty("slug").GetString() == AuthTestHelpers.DemoSchoolSlug);
    }

    [Fact]
    public async Task GetDashboard_WhenCrossSchool_Returns404()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/dashboard");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
        Assert.Contains(SchoolPortalErrorCodes.SchoolNotFound, ReadErrorCodes(json));
    }

    [Fact]
    public async Task UpdateProfile_WhenSchoolOwner_DoesNotChangeStatus()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword));

        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var profileResponse = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profileJson = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
        var originalStatus = profileJson.GetProperty("data").GetProperty("status").GetInt32();
        var originalNameEn = profileJson.GetProperty("data").GetProperty("nameEn").GetString();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/profile",
            new
            {
                nameAr = "مدرسة محدثة",
                nameEn = originalNameEn ?? "Updated School",
                shortDescriptionAr = "وصف",
                shortDescriptionEn = "Description",
                fullDescriptionAr = (string?)null,
                fullDescriptionEn = (string?)null,
                schoolType = 2,
                genderType = 3,
                foundedYear = 1998,
                studentCount = 1200,
                publicPhone = "+201000000000",
                publicEmail = "info@example.com",
                websiteUrl = "https://example.com",
                whatsAppNumber = (string?)null,
                seoTitleAr = (string?)null,
                seoTitleEn = (string?)null,
                seoDescriptionAr = (string?)null,
                seoDescriptionEn = (string?)null,
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedJson = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(updatedJson.GetProperty("succeeded").GetBoolean());
        Assert.Equal(originalStatus, updatedJson.GetProperty("data").GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task AddSchoolAdmin_WhenSchoolAdmin_Returns403OwnerRequired()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolAdminEmail,
            AuthTestHelpers.DefaultPassword));

        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/school-admins",
            new { email = "someone@schoolera.local" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
        Assert.Contains(SchoolPortalErrorCodes.OwnerRequired, ReadErrorCodes(json));
    }

    private static async Task<Guid?> TryGetDemoSchoolIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/school-portal/schools");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!json.GetProperty("succeeded").GetBoolean())
        {
            return null;
        }

        foreach (var item in json.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("slug").GetString() == AuthTestHelpers.DemoSchoolSlug)
            {
                return item.GetProperty("id").GetGuid();
            }
        }

        return null;
    }

    private static IEnumerable<string> ReadErrorCodes(JsonElement json) =>
        json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()!);
}
