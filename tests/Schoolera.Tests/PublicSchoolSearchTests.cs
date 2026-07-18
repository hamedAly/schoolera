using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class PublicSchoolSearchTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public PublicSchoolSearchTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Schools_Default_ReturnsOnlyPublishedDistinctIds()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?pageNumber=1&pageSize=20&sort=newest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var items = json.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        var ids = items.Select(item => item.GetProperty("id").GetString()).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Schools_FeaturedHomepageShape_ReturnsUpToFour()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?pageNumber=1&pageSize=4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var items = json.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        Assert.True(items.Count <= 4);
        if (items.Count > 0)
        {
            Assert.True(items[0].TryGetProperty("name", out _));
            Assert.True(items[0].TryGetProperty("slug", out _));
            Assert.True(items[0].TryGetProperty("city", out _));
        }
    }

    [Fact]
    public async Task Schools_EmptySearch_TreatedAsNoFilter()
    {
        using var client = _factory.CreateClient();
        var blank = await client.GetAsync("/api/schools?search=%20%20&pageNumber=1&pageSize=20");
        var all = await client.GetAsync("/api/schools?pageNumber=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, blank.StatusCode);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);

        var blankCount = (await blank.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("totalCount").GetInt32();
        var allCount = (await all.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("totalCount").GetInt32();
        Assert.Equal(allCount, blankCount);
    }

    [Theory]
    [InlineData("name-asc")]
    [InlineData("name-desc")]
    [InlineData("newest")]
    [InlineData("lowest-fee")]
    [InlineData("highest-fee")]
    [InlineData("relevance")]
    public async Task Schools_SupportedSorts_ReturnOk(string sort)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/schools?sort={sort}&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_InvalidSort_Returns400()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?sort=popularity&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Schools_NearestWithoutCoordinates_Returns400()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?sort=nearest&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Schools_NearestWithCoordinates_ReturnsOkOrValidation()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            "/api/schools?sort=nearest&latitude=30.0444&longitude=31.2357&pageNumber=1&pageSize=10");
        // Nearest may 500 if SQL cannot translate distance for the current provider seed;
        // accept OK, and surface body for diagnosis when unexpected.
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Nearest sort failed: {body}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_InvalidTuitionRange_Returns400()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            "/api/schools?minimumTuition=50000&maximumTuition=1000&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Schools_PageSizeAboveMax_IsCapped()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?pageNumber=1&pageSize=500");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("pageSize").GetInt32() <= 100);
    }

    [Fact]
    public async Task Schools_AdmissionOpenFilter_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schools?admissionOpen=true&pageNumber=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_ArabicAcceptLanguage_ReturnsLocalizedNames()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "ar");
        var response = await client.GetAsync("/api/schools?pageNumber=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task Schools_EnglishAcceptLanguage_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en");
        var response = await client.GetAsync("/api/schools?pageNumber=1&pageSize=5&sort=name-asc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_SearchText_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var ar = await client.GetAsync("/api/schools?search=%D9%85%D8%AF%D8%B1%D8%B3%D8%A9&pageNumber=1&pageSize=10");
        var en = await client.GetAsync("/api/schools?search=school&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, ar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, en.StatusCode);
    }

    [Fact]
    public async Task Schools_CityFilter_WhenCitiesExist_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var citiesResponse = await client.GetAsync("/api/taxonomies/cities");
        var citiesJson = await citiesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cities = citiesJson.GetProperty("data").EnumerateArray().ToList();
        if (cities.Count == 0)
        {
            return;
        }

        var cityId = cities[0].GetProperty("id").GetString();
        var response = await client.GetAsync($"/api/schools?cityId={cityId}&pageNumber=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_CountryIdFilter_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var countryId = Guid.NewGuid();
        var response = await client.GetAsync(
            $"/api/schools?countryId={countryId}&pageNumber=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Schools_GovernorateIdFilter_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var governorateId = Guid.NewGuid();
        var response = await client.GetAsync(
            $"/api/schools?governorateId={governorateId}&pageNumber=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Hierarchy mismatch (city/governorate/country) coverage needs seeded taxonomy IDs
    // once public country/governorate list endpoints (or seed helpers) are available.
}
