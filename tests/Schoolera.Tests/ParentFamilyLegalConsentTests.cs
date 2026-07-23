using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Legal.Constants;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class ParentFamilyLegalConsentTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly SchooleraWebApplicationFactory _factory;

    public ParentFamilyLegalConsentTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LegalCurrent_ReturnsTermsAndPrivacyLinks()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/legal/current");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal("/terms", data.GetProperty("terms").GetProperty("path").GetString());
        Assert.Equal("/privacy", data.GetProperty("privacy").GetProperty("path").GetString());
        Assert.True(data.GetProperty("terms").GetProperty("versionNumber").GetInt32() >= 1);
        Assert.True(data.GetProperty("privacy").GetProperty("versionNumber").GetInt32() >= 1);
        Assert.False(data.GetProperty("terms").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task RegisterParent_MissingPrivacy_ReturnsLegalErrorCode()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

        var email = $"legal-parent-{Guid.NewGuid():N}@example.invalid";
        var response = await client.PostAsJsonAsync(
            "/api/auth/register/parent",
            new
            {
                firstName = "Legal",
                lastName = "Parent",
                email,
                phoneNumber = $"+201{Random.Shared.NextInt64(100000000, 999999999)}",
                password = "Schoolera@Dev1",
                confirmPassword = "Schoolera@Dev1",
                termsAccepted = true,
                privacyAccepted = false,
                preferredLanguage = "ar",
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("succeeded").GetBoolean());
        var codes = json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains(LegalErrorCodes.PrivacyRequired, codes);
    }

    [Fact]
    public async Task RegisterParent_WithTermsAndPrivacy_Succeeds()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);

        var email = $"legal-ok-{Guid.NewGuid():N}@example.invalid";
        var response = await client.PostAsJsonAsync(
            "/api/auth/register/parent",
            new
            {
                firstName = "Legal",
                lastName = "Ok",
                email,
                phoneNumber = $"+201{Random.Shared.NextInt64(100000000, 999999999)}",
                password = "Schoolera@Dev1",
                confirmPassword = "Schoolera@Dev1",
                termsAccepted = true,
                privacyAccepted = true,
                preferredLanguage = "ar",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        Assert.Equal(email, json.GetProperty("data").GetProperty("email").GetString(), ignoreCase: true);
    }

    [Fact]
    public async Task ParentProfile_GuardianFields_AndMaskedIdentityPreservation()
    {
        await Gate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            await AuthTestHelpers.TryLoginAsync(
                client,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);

            var cities = await client.GetAsync("/api/taxonomies/cities");
            cities.EnsureSuccessStatusCode();
            var city = (await cities.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray().First();
            var cityId = city.GetProperty("id").GetGuid();
            var districts = await client.GetAsync($"/api/taxonomies/cities/{cityId}/districts");
            var districtId = (await districts.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

            var identity = $"2990101{Random.Shared.Next(100000, 999999)}";
            var put = await client.PutAsJsonAsync(
                "/api/parent/profile",
                new
                {
                    firstName = "Parent",
                    lastName = "Demo",
                    phone = "+201000000001",
                    alternatePhone = "+201000000099",
                    addressLine = "Family address",
                    qualification = "Bachelor",
                    occupation = "Engineer",
                    cityId,
                    districtId,
                    preferredContactMethod = 1,
                    preferredLanguage = "ar",
                    father = new
                    {
                        fullName = "Father Name",
                        phone = "+201111111111",
                        email = "father@example.invalid",
                        occupation = "Teacher",
                        qualification = "Master",
                        identityType = 1,
                        identityValue = identity,
                    },
                    mother = new
                    {
                        fullName = "Mother Name",
                        phone = "+201222222222",
                        email = "mother@example.invalid",
                        occupation = "Doctor",
                        qualification = "PhD",
                        identityType = (int?)null,
                        identityValue = (string?)null,
                    },
                });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            var profile = (await put.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal("Bachelor", profile.GetProperty("qualification").GetString());
            Assert.Equal("Father Name", profile.GetProperty("father").GetProperty("fullName").GetString());
            var masked = profile.GetProperty("father").GetProperty("maskedIdentity").GetString()!;
            Assert.StartsWith("************", masked, StringComparison.Ordinal);
            Assert.DoesNotContain(identity, masked, StringComparison.Ordinal);
            Assert.False(profile.GetProperty("father").TryGetProperty("protectedIdentityValue", out _));

            var preserve = await client.PutAsJsonAsync(
                "/api/parent/profile",
                new
                {
                    firstName = "Parent",
                    lastName = "Demo",
                    phone = "+201000000001",
                    alternatePhone = "+201000000099",
                    addressLine = "Family address",
                    qualification = "Bachelor",
                    occupation = "Engineer",
                    cityId,
                    districtId,
                    preferredContactMethod = 1,
                    preferredLanguage = "ar",
                    father = new
                    {
                        fullName = "Father Name",
                        phone = "+201111111111",
                        email = "father@example.invalid",
                        occupation = "Teacher",
                        qualification = "Master",
                        identityType = 1,
                        identityValue = masked,
                    },
                    mother = new
                    {
                        fullName = "Mother Name",
                        phone = "+201222222222",
                        email = "mother@example.invalid",
                        occupation = "Doctor",
                        qualification = "PhD",
                        identityType = (int?)null,
                        identityValue = (string?)null,
                    },
                });
            Assert.Equal(HttpStatusCode.OK, preserve.StatusCode);
            var preserved = (await preserve.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal(masked, preserved.GetProperty("father").GetProperty("maskedIdentity").GetString());
        }
        finally
        {
            Gate.Release();
        }
    }

    [Fact]
    public async Task SubmitAdmission_MissingConsent_ReturnsLegalErrorCode()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = false });

            Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);
            var json = await submit.Content.ReadFromJsonAsync<JsonElement>();
            var codes = json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()).ToArray();
            Assert.Contains(LegalErrorCodes.PrivacyRequired, codes);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task SubmitAdmission_SnapshotsSchoolVisibleParentGuardianFields()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var cities = await client.GetAsync("/api/taxonomies/cities");
            var cityId = (await cities.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();
            var districtId = (await (await client.GetAsync($"/api/taxonomies/cities/{cityId}/districts"))
                    .Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

            var fatherIdentity = $"2880101{Random.Shared.Next(100000, 999999)}";
            (await client.PutAsJsonAsync(
                "/api/parent/profile",
                new
                {
                    firstName = "Parent",
                    lastName = "Snapshot",
                    phone = "+201000000001",
                    alternatePhone = "+201000000088",
                    addressLine = "Snapshot address",
                    qualification = "BA",
                    occupation = "Analyst",
                    cityId,
                    districtId,
                    preferredContactMethod = 1,
                    preferredLanguage = "ar",
                    father = new
                    {
                        fullName = "Snapshot Father",
                        phone = "+201333333333",
                        email = "snap-father@example.invalid",
                        occupation = "Farmer",
                        qualification = "Diploma",
                        identityType = 1,
                        identityValue = fatherIdentity,
                    },
                    mother = new
                    {
                        fullName = "Snapshot Mother",
                        phone = "+201444444444",
                        email = "snap-mother@example.invalid",
                        occupation = "Nurse",
                        qualification = "BSc",
                        identityType = (int?)null,
                        identityValue = (string?)null,
                    },
                })).EnsureSuccessStatusCode();

            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                AdmissionTestHelpers.SubmitConsentBody);
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            var application = await db.AdmissionApplications
                .AsNoTracking()
                .FirstAsync(item => item.Id == applicationId);

            Assert.Equal("Parent Snapshot", application.SubmittedParentDisplayName);
            Assert.Equal("+201000000088", application.SubmittedParentAlternatePhone);
            Assert.Equal("Snapshot Father", application.SubmittedFatherFullName);
            Assert.Equal("Snapshot Mother", application.SubmittedMotherFullName);
            Assert.NotNull(application.SubmittedFatherMaskedIdentity);
            Assert.DoesNotContain(
                fatherIdentity,
                application.SubmittedFatherMaskedIdentity!,
                StringComparison.Ordinal);
            Assert.Null(application.GetType().GetProperty("SubmittedFatherProtectedIdentityValue"));
            Assert.Null(application.GetType().GetProperty("SubmittedFatherIdentityLookupHash"));
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}
