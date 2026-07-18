using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AuthIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AuthIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_WhenAnonymous_ReturnsSucceededWithNullData()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("data").ValueKind);
    }

    [Fact]
    public async Task PortalParent_WithoutAuth_Returns401JsonResult()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/portal/parent");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
        Assert.Contains("auth.unauthorized", ReadErrorCodes(json));
    }

    private static IEnumerable<string> ReadErrorCodes(JsonElement json)
    {
        return json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()!);
    }
}
