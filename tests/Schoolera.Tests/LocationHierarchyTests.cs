using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class LocationHierarchyTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public LocationHierarchyTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Public_Countries_IncludeEgyptWithCodeEg()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/taxonomies/countries");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var egypt = json.GetProperty("data").EnumerateArray()
            .FirstOrDefault(item => item.GetProperty("code").GetString() == "EG");
        Assert.Equal(JsonValueKind.Object, egypt.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(egypt.GetProperty("id").GetString()));
        Assert.Equal("egypt", egypt.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Public_GovernoratesByCountry_ReturnsEgyptianGovernorates()
    {
        using var client = _factory.CreateClient();
        var countries = await client.GetFromJsonAsync<JsonElement>("/api/taxonomies/countries");
        var egyptId = countries.GetProperty("data").EnumerateArray()
            .First(item => item.GetProperty("code").GetString() == "EG")
            .GetProperty("id")
            .GetString();

        var response = await client.GetAsync($"/api/taxonomies/countries/{egyptId}/governorates");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var slugs = json.GetProperty("data").EnumerateArray()
            .Select(item => item.GetProperty("slug").GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("cairo", slugs);
        Assert.Contains("giza", slugs);
        Assert.Contains("alexandria", slugs);
        Assert.True(slugs.Count >= 27);
    }

    [Fact]
    public async Task Public_CitiesByGovernorate_ReturnsMappedCairoCity()
    {
        using var client = _factory.CreateClient();
        var (egyptId, cairoGovId) = await ResolveEgyptCairoAsync(client);

        var response = await client.GetAsync($"/api/taxonomies/governorates/{cairoGovId}/cities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var slugs = json.GetProperty("data").EnumerateArray()
            .Select(item => item.GetProperty("slug").GetString())
            .ToArray();
        Assert.Contains("cairo", slugs);
    }

    [Fact]
    public async Task Public_Cities_StillReturnsLegacyCompatibleList()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/taxonomies/cities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var slugs = json.GetProperty("data").EnumerateArray()
            .Select(item => item.GetProperty("slug").GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("cairo", slugs);
        Assert.Contains("giza", slugs);
        Assert.Contains("alexandria", slugs);
    }

    [Fact]
    public async Task Schools_FilterByCountry_ReturnsOkDistinctIds()
    {
        using var client = _factory.CreateClient();
        var (egyptId, _) = await ResolveEgyptCairoAsync(client);
        var response = await client.GetAsync($"/api/schools?countryId={egyptId}&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var ids = json.GetProperty("data").GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString())
            .ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Schools_FilterByGovernorate_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var (_, cairoGovId) = await ResolveEgyptCairoAsync(client);
        var response = await client.GetAsync($"/api/schools?governorateId={cairoGovId}&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_InvalidGovernorateForCountry_Returns400()
    {
        using var client = _factory.CreateClient();
        var (egyptId, _) = await ResolveEgyptCairoAsync(client);
        var response = await client.GetAsync(
            $"/api/schools?countryId={egyptId}&governorateId={Guid.NewGuid()}&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Schools_CityStillFilters_Compatible()
    {
        using var client = _factory.CreateClient();
        var cities = await client.GetFromJsonAsync<JsonElement>("/api/taxonomies/cities");
        var cairoId = cities.GetProperty("data").EnumerateArray()
            .First(item => item.GetProperty("slug").GetString() == "cairo")
            .GetProperty("id")
            .GetString();

        var response = await client.GetAsync($"/api/schools?cityId={cairoId}&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_InvalidLatitude_Returns400()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?latitude=999&longitude=31&pageSize=5");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreateCountry_Unauthorized_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/admin/taxonomies/countries",
            new { code = "XX", nameAr = "اختبار", nameEn = "Test", slug = "test-xx", sortOrder = 99 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<(string EgyptId, string CairoGovId)> ResolveEgyptCairoAsync(HttpClient client)
    {
        var countries = await client.GetFromJsonAsync<JsonElement>("/api/taxonomies/countries");
        var egyptId = countries.GetProperty("data").EnumerateArray()
            .First(item => item.GetProperty("code").GetString() == "EG")
            .GetProperty("id")
            .GetString()!;

        var governorates = await client.GetFromJsonAsync<JsonElement>(
            $"/api/taxonomies/countries/{egyptId}/governorates");
        var cairoGovId = governorates.GetProperty("data").EnumerateArray()
            .First(item => item.GetProperty("slug").GetString() == "cairo")
            .GetProperty("id")
            .GetString()!;

        return (egyptId, cairoGovId);
    }
}
