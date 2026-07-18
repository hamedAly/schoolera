using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

/// <summary>
/// Authorization-boundary checks for onboarding endpoints. Anonymous requests are rejected by
/// the authorization middleware (401) before reaching the action, so these do not exercise the
/// antiforgery-over-HTTP path that requires an authenticated SSL request.
/// </summary>
[Collection(WebApplicationFactoryCollection.Name)]
public sealed class OnboardingAuthorizationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public OnboardingAuthorizationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyApplication_WhenAnonymous_Returns401Json()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/school-onboarding/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task GetDocumentTypes_WhenAnonymous_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/school-onboarding/document-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminList_WhenAnonymous_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/school-onboarding");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveOrganization_WhenAnonymous_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync(
            "/api/school-onboarding/me/organization",
            new { organizationNameAr = "مؤسسة", countryCode = "EG", registrationOrLicenseNumber = "REG-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
