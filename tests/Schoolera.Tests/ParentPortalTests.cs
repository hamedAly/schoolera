using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class ParentAuthorizationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public ParentAuthorizationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/parent/dashboard")]
    [InlineData("/api/parent/profile")]
    [InlineData("/api/parent/children")]
    public async Task ParentGet_Anonymous_Returns401(string path)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthTestHelpers.SchoolOwnerEmail)]
    [InlineData(AuthTestHelpers.SchoolAdminEmail)]
    [InlineData(AuthTestHelpers.PlatformAdminEmail)]
    [InlineData(AuthTestHelpers.SupportAgentEmail)]
    public async Task ParentGet_NonParentRoles_Return403(string email)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword);
        var response = await client.GetAsync("/api/parent/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ParentGet_Parent_Returns200()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.ParentEmail,
            AuthTestHelpers.DefaultPassword);
        var response = await client.GetAsync("/api/parent/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class ParentIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly SchooleraWebApplicationFactory _factory;

    public ParentIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_ApplicationsUnavailable_AndChildCounts()
    {
        using var client = await CreateParentClientAsync();
        var response = await client.GetAsync("/api/parent/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data");
        Assert.True(data.GetProperty("applicationsAvailable").GetBoolean());
        Assert.Equal(JsonValueKind.Number, data.GetProperty("totalApplications").ValueKind);
        Assert.True(data.TryGetProperty("draftApplications", out _));
        Assert.True(data.TryGetProperty("submittedApplications", out _));
        Assert.True(data.TryGetProperty("underReviewApplications", out _));
        Assert.True(data.TryGetProperty("acceptedApplications", out _));
        Assert.True(data.TryGetProperty("rejectedApplications", out _));
        Assert.True(data.TryGetProperty("cancelledApplications", out _));
        Assert.True(data.TryGetProperty("recentActivityAvailable", out _));
        Assert.True(data.TryGetProperty("childCount", out _));
        Assert.True(data.TryGetProperty("activeChildCount", out _));
    }

    [Fact]
    public async Task Profile_UpdateAndRead_RoundTrip()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var cities = await client.GetAsync("/api/taxonomies/cities");
            cities.EnsureSuccessStatusCode();
            var city = (await cities.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray().First();
            var cityId = city.GetProperty("id").GetGuid();
            var districts = await client.GetAsync($"/api/taxonomies/cities/{cityId}/districts");
            var districtId = (await districts.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

            var put = await client.PutAsJsonAsync(
                "/api/parent/profile",
                new
                {
                    firstName = "Parent",
                    lastName = "Demo",
                    phone = "+201000000001",
                    alternatePhone = "+201000000099",
                    addressLine = "Test address",
                    cityId,
                    districtId,
                    preferredContactMethod = 1,
                    preferredLanguage = "ar",
                });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            var putJson = await put.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(putJson.GetProperty("succeeded").GetBoolean());
            Assert.False(putJson.GetProperty("data").TryGetProperty("userId", out _));

            var get = await client.GetAsync("/api/parent/profile");
            var profile = (await get.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal("Test address", profile.GetProperty("addressLine").GetString());
            Assert.True(profile.GetProperty("isComplete").GetBoolean());
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task Child_CreateListUpdateReplaceIdentity_AndMasking()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var gradeId = await ResolveActiveGradeIdAsync(client);
            var identity = $"2990101{Random.Shared.Next(100000, 999999)}";

            var create = await client.PostAsJsonAsync(
                "/api/parent/children",
                new
                {
                    fullName = "Child One",
                    identityType = 1,
                    identityValue = identity,
                    birthDate = "2015-05-01",
                    gender = 1,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = false,
                    specialNeedsNotes = (string?)null,
                });
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);
            var created = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            var childId = created.GetProperty("id").GetGuid();
            var masked = created.GetProperty("maskedIdentity").GetString()!;
            Assert.StartsWith("************", masked, StringComparison.Ordinal);
            Assert.DoesNotContain(identity, masked, StringComparison.Ordinal);
            Assert.False(created.TryGetProperty("protectedIdentityValue", out _));
            Assert.False(created.TryGetProperty("identityLookupHash", out _));

            var list = await client.GetAsync("/api/parent/children");
            var items = (await list.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray().ToList();
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == childId);

            var update = await client.PutAsJsonAsync(
                $"/api/parent/children/{childId}",
                new
                {
                    fullName = "Child One Updated",
                    birthDate = "2015-05-01",
                    gender = 1,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = true,
                    specialNeedsNotes = "Notes",
                    identityType = (int?)null,
                    identityValue = (string?)null,
                });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var updated = (await update.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal("Child One Updated", updated.GetProperty("fullName").GetString());
            Assert.Equal(masked, updated.GetProperty("maskedIdentity").GetString());

            var newIdentity = $"2980202{Random.Shared.Next(100000, 999999)}";
            var replace = await client.PutAsJsonAsync(
                $"/api/parent/children/{childId}",
                new
                {
                    fullName = "Child One Updated",
                    birthDate = "2015-05-01",
                    gender = 1,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = true,
                    specialNeedsNotes = "Notes",
                    identityType = 1,
                    identityValue = newIdentity,
                });
            Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
            var replaced = (await replace.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.NotEqual(masked, replaced.GetProperty("maskedIdentity").GetString());
            Assert.DoesNotContain(newIdentity, replaced.GetProperty("maskedIdentity").GetString()!);

            var delete = await client.DeleteAsync($"/api/parent/children/{childId}");
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
            var listAfter = await client.GetAsync("/api/parent/children");
            var remaining = (await listAfter.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid());
            Assert.DoesNotContain(childId, remaining);
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task Child_DuplicateIdentity_FutureBirth_InvalidGrade_SpecialNeedsRule()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var gradeId = await ResolveActiveGradeIdAsync(client);
            var identity = $"2970303{Random.Shared.Next(100000, 999999)}";

            var create = await client.PostAsJsonAsync(
                "/api/parent/children",
                new
                {
                    fullName = "Dup Child",
                    identityType = 1,
                    identityValue = identity,
                    birthDate = "2014-01-01",
                    gender = 2,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = false,
                });
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);

            var dup = await client.PostAsJsonAsync(
                "/api/parent/children",
                new
                {
                    fullName = "Dup Child 2",
                    identityType = 1,
                    identityValue = identity,
                    birthDate = "2014-01-01",
                    gender = 2,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = false,
                });
            Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
            var dupCodes = (await dup.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString());
            Assert.Contains("parent.child.identityAlreadyExists", dupCodes);

            var future = await client.PostAsJsonAsync(
                "/api/parent/children",
                new
                {
                    fullName = "Future",
                    identityType = 1,
                    identityValue = $"2960404{Random.Shared.Next(100000, 999999)}",
                    birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd"),
                    gender = 1,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = false,
                });
            Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);

            var badGrade = await client.PostAsJsonAsync(
                "/api/parent/children",
                new
                {
                    fullName = "Bad Grade",
                    identityType = 1,
                    identityValue = $"2950505{Random.Shared.Next(100000, 999999)}",
                    birthDate = "2014-01-01",
                    gender = 1,
                    currentGradeId = Guid.NewGuid(),
                    hasSpecialNeeds = false,
                });
            Assert.Equal(HttpStatusCode.BadRequest, badGrade.StatusCode);

            var notes = await client.PostAsJsonAsync(
                "/api/parent/children",
                new
                {
                    fullName = "Notes",
                    identityType = 1,
                    identityValue = $"2940606{Random.Shared.Next(100000, 999999)}",
                    birthDate = "2014-01-01",
                    gender = 1,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = false,
                    specialNeedsNotes = "should fail",
                });
            Assert.Equal(HttpStatusCode.BadRequest, notes.StatusCode);
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task Child_NonOwned_ReturnsSame404()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var unknown = await client.GetAsync($"/api/parent/children/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            var codes = (await unknown.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString());
            Assert.Contains("parent.child.notFound", codes);
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task Child_Extensions_CreateUpdate_ListExcludesSensitiveFields()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = await CreateParentClientAsync();
            var gradeId = await ResolveActiveGradeIdAsync(client);
            var identity = $"2930707{Random.Shared.Next(100000, 999999)}";

            var create = await client.PostAsJsonAsync(
                "/api/parent/children",
                new
                {
                    fullName = "Extended Child",
                    identityType = 1,
                    identityValue = identity,
                    birthDate = "2015-05-01",
                    gender = 1,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = true,
                    specialNeedsNotes = "Needs support",
                    currentSchoolName = "Al Noor School",
                    preferredStudyLanguage = 2,
                    skills = "Reading",
                    hobbies = "Chess",
                    strengths = "Curiosity",
                    improvementAreas = "Writing",
                    healthNotes = "Allergy note for parent only",
                });
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);
            var created = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            var childId = created.GetProperty("id").GetGuid();
            Assert.Equal("Al Noor School", created.GetProperty("currentSchoolName").GetString());
            Assert.Equal(2, created.GetProperty("preferredStudyLanguage").GetInt32());
            Assert.Equal("Reading", created.GetProperty("skills").GetString());
            Assert.Equal("Allergy note for parent only", created.GetProperty("healthNotes").GetString());
            Assert.Equal("Needs support", created.GetProperty("specialNeedsNotes").GetString());

            var list = await client.GetAsync("/api/parent/children");
            var item = (await list.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray()
                .First(x => x.GetProperty("id").GetGuid() == childId);
            Assert.Equal("Al Noor School", item.GetProperty("currentSchoolName").GetString());
            Assert.Equal(2, item.GetProperty("preferredStudyLanguage").GetInt32());
            Assert.False(item.TryGetProperty("healthNotes", out _));
            Assert.False(item.TryGetProperty("specialNeedsNotes", out _));
            Assert.False(item.TryGetProperty("skills", out _));
            Assert.False(item.TryGetProperty("hobbies", out _));
            Assert.False(item.TryGetProperty("strengths", out _));
            Assert.False(item.TryGetProperty("improvementAreas", out _));

            var update = await client.PutAsJsonAsync(
                $"/api/parent/children/{childId}",
                new
                {
                    fullName = "Extended Child Updated",
                    birthDate = "2015-05-01",
                    gender = 1,
                    currentGradeId = gradeId,
                    hasSpecialNeeds = true,
                    specialNeedsNotes = "Needs support",
                    currentSchoolName = "New School",
                    preferredStudyLanguage = 1,
                    skills = "Math",
                    hobbies = "Swimming",
                    strengths = "Teamwork",
                    improvementAreas = "Focus",
                    healthNotes = "Updated health",
                    identityType = (int?)null,
                    identityValue = (string?)null,
                });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var updated = (await update.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal("New School", updated.GetProperty("currentSchoolName").GetString());
            Assert.Equal(1, updated.GetProperty("preferredStudyLanguage").GetInt32());
            Assert.Equal("Updated health", updated.GetProperty("healthNotes").GetString());
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<HttpClient> CreateParentClientAsync()
    {
        var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.ParentEmail,
            AuthTestHelpers.DefaultPassword);
        return client;
    }

    private static async Task<Guid> ResolveActiveGradeIdAsync(HttpClient client)
    {
        var stages = await client.GetAsync("/api/taxonomies/educational-stages");
        stages.EnsureSuccessStatusCode();
        var stageId = (await stages.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();
        var grades = await client.GetAsync($"/api/taxonomies/educational-stages/{stageId}/grades");
        grades.EnsureSuccessStatusCode();
        return (await grades.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();
    }
}
