using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class SchoolCatalogIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public SchoolCatalogIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TaxonomiesCities_ReturnsResultEnvelope()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/taxonomies/cities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task Schools_ReturnsPagedResultEnvelope()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?pageNumber=1&pageSize=4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        Assert.True(json.GetProperty("data").TryGetProperty("items", out _));
        Assert.True(json.GetProperty("data").TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task SchoolsUnknownSlug_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools/does-not-exist-slug");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminTaxonomyWrite_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/admin/taxonomies/cities",
            new { nameAr = "Test", nameEn = "Test", slug = "test-city", sortOrder = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
