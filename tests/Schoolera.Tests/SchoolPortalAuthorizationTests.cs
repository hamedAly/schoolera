using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

/// <summary>
/// Authorization-boundary checks for school portal endpoints. Anonymous requests are rejected
/// by the authorization middleware (401) before reaching the action.
/// </summary>
[Collection(WebApplicationFactoryCollection.Name)]
public sealed class SchoolPortalAuthorizationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public SchoolPortalAuthorizationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListSchools_WhenAnonymous_Returns401Json()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/school-portal/schools");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task GetDashboard_WhenAnonymous_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WhenAnonymous_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync(
            $"/api/school-portal/schools/{Guid.NewGuid()}/profile",
            new
            {
                nameAr = "مدرسة",
                nameEn = "School",
                schoolType = 2,
                genderType = 3,
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListSchools_WhenParent_Returns403()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        if (!await AuthTestHelpers.TryLoginAsync(client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword))
        {
            return;
        }

        var response = await client.GetAsync("/api/school-portal/schools");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
