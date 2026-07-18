using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdminPlatformAuthorizationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdminPlatformAuthorizationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/api/admin/schools")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/audit")]
    public async Task AdminEndpoints_WhenAnonymous_Return401(string path)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task Dashboard_WhenParent_Returns403()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdminPlatformIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdminPlatformIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_WhenPlatformAdmin_ReturnsRealCountsAndAdmissionMetrics()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());

        var data = json.GetProperty("data");
        Assert.True(data.GetProperty("totalSchools").GetInt32() >= 0);
        Assert.True(data.GetProperty("totalUsers").GetInt32() >= 1);
        Assert.True(data.GetProperty("admissionsAvailable").GetBoolean());
        Assert.True(data.GetProperty("admissionApplicationsCount").ValueKind == JsonValueKind.Number
            && data.GetProperty("admissionApplicationsCount").GetInt32() >= 0);
        Assert.True(data.GetProperty("admissionPendingSchoolReviewCount").ValueKind == JsonValueKind.Number
            && data.GetProperty("admissionPendingSchoolReviewCount").GetInt32() >= 0);
    }

    [Fact]
    public async Task ListSchools_WhenPlatformAdmin_ReturnsPagedResult()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync("/api/admin/schools?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        Assert.True(json.GetProperty("data").GetProperty("totalCount").GetInt32() >= 0);
        Assert.Equal(JsonValueKind.Array, json.GetProperty("data").GetProperty("items").ValueKind);
    }

    [Fact]
    public async Task ListUsers_WhenPlatformAdmin_ReturnsSeededAdmin()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(AuthTestHelpers.PlatformAdminEmail)}&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        Assert.True(json.GetProperty("data").GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task UpdateUserStatus_CannotModifySelf()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        var meResponse = await client.GetAsync("/api/auth/me");
        var meJson = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = meJson.GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{userId}/status",
            new { accountStatus = "Suspended" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            "admin.cannotModifySelf",
            json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()));
    }
}
