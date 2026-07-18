using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionRequirementsTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionRequirementsTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ResolveApplicable_PicksMostSpecificPublishedDefinitionPerCode()
    {
        var schoolId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var schoolDefault = CreateRequirement(
            schoolId, "birth-cert", yearId: null, branchId: null, stageId: null, gradeId: null,
            sortOrder: 1, userId: userId);
        var gradeYear = CreateRequirement(
            schoolId, "birth-cert", yearId, branchId: null, stageId: null, gradeId,
            sortOrder: 2, userId: userId);
        var branchGradeYear = CreateRequirement(
            schoolId, "birth-cert", yearId, branchId, stageId: null, gradeId,
            sortOrder: 3, userId: userId);

        var applicable = AdmissionRequirementCatalog.ResolveApplicable(
            [schoolDefault, gradeYear, branchGradeYear],
            branchId,
            stageId,
            gradeId,
            yearId);

        Assert.Single(applicable);
        Assert.Equal(branchGradeYear.Id, applicable[0].Id);
    }

    [Fact]
    public async Task SchoolPortalRequirements_Parent_Returns403()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword);

        var profile = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        profile.EnsureSuccessStatusCode();
        var schoolId = (await profile.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/admission-requirements");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRequirement_WithoutCsrf_Returns400()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword);

        var schoolId = await GetDemoSchoolIdAsync(client);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/admission-requirements",
            new
            {
                requirementCode = "test-doc",
                kind = (int)AdmissionRequirementKind.ApplicationDocument,
                nameAr = "مستند",
                nameEn = "Document",
                isRequired = true,
                sortOrder = 0,
                documentCode = (int)AdmissionRequiredDocumentCode.BirthCertificate,
                allowedFileExtensions = new[] { ".pdf" },
                allowChildVaultCopy = true,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EnsureSnapshots_IsIdempotentOnCreateAndUpdate()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            // Resolve catalog first — EnsureDemo deactivates published requirements on every call.
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                client,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);
            var context = await AdmissionTestHelpers.ResolveCreateContextFromPublicProfileAsync(client);

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();

            var school = await db.Schools.AsNoTracking()
                .FirstAsync(item => item.Slug == AuthTestHelpers.DemoSchoolSlug);
            var ownerId = await db.Users.AsNoTracking()
                .Where(user => user.Email == AuthTestHelpers.SchoolOwnerEmail)
                .Select(user => user.Id)
                .FirstAsync();

            var code = $"parent-occupation-{Guid.NewGuid():N}";
            var requirement = new SchoolAdmissionRequirement(
                school.Id,
                code,
                AdmissionRequirementKind.ParentProfileField,
                "مهنة ولي الأمر",
                "Parent occupation",
                null,
                null,
                isRequired: true,
                sortOrder: 0,
                schoolBranchId: null,
                educationalStageId: null,
                gradeId: null,
                academicYearId: null,
                AdmissionProfileFieldCode.ParentOccupation,
                documentCode: null,
                allowedFileExtensions: null,
                maxFileSizeBytes: null,
                allowChildVaultCopy: false,
                ownerId);
            requirement.Publish(ownerId);
            db.SchoolAdmissionRequirements.Add(requirement);
            await db.SaveChangesAsync();

            try
            {
                var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
                var draftJson = await AdmissionTestHelpers.CreateDraftAsync(
                    client, context, childId, context.Slots[0]);
                var draftId = draftJson.GetProperty("id").GetGuid();

                var snapshotCount = await db.AdmissionApplicationRequirementSnapshots
                    .AsNoTracking()
                    .CountAsync(snapshot => snapshot.AdmissionApplicationId == draftId);
                Assert.True(snapshotCount > 0);

                var updateBody = new
                {
                    schoolBranchId = context.Slots[0].BranchId,
                    educationalStageId = context.Slots[0].EducationalStageId,
                    gradeId = context.Slots[0].GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "updated",
                    rowVersion = (byte[]?)null,
                };
                var updateResponse = await client.PutAsJsonAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{draftId}",
                    updateBody);
                updateResponse.EnsureSuccessStatusCode();

                var afterUpdate = await db.AdmissionApplicationRequirementSnapshots
                    .AsNoTracking()
                    .CountAsync(snapshot => snapshot.AdmissionApplicationId == draftId);
                Assert.Equal(snapshotCount, afterUpdate);
            }
            finally
            {
                requirement.Deactivate(ownerId);
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Submit_WithIncompleteRequirements_ReturnsRequirementsIncompletePayload()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            // Resolve catalog first — EnsureDemo deactivates published requirements on every call.
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                client,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);
            var context = await AdmissionTestHelpers.ResolveCreateContextFromPublicProfileAsync(client);

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();

            var school = await db.Schools.AsNoTracking()
                .FirstAsync(item => item.Slug == AuthTestHelpers.DemoSchoolSlug);
            var ownerId = await db.Users.AsNoTracking()
                .Where(user => user.Email == AuthTestHelpers.SchoolOwnerEmail)
                .Select(user => user.Id)
                .FirstAsync();

            var code = $"birth-cert-required-{Guid.NewGuid():N}";
            var requirement = new SchoolAdmissionRequirement(
                school.Id,
                code,
                AdmissionRequirementKind.ApplicationDocument,
                "شهادة الميلاد",
                "Birth certificate",
                null,
                null,
                isRequired: true,
                sortOrder: 0,
                null,
                null,
                null,
                null,
                null,
                AdmissionRequiredDocumentCode.BirthCertificate,
                ".pdf",
                5_000_000,
                false,
                ownerId);
            requirement.Publish(ownerId);
            db.SchoolAdmissionRequirements.Add(requirement);
            await db.SaveChangesAsync();

            try
            {
                var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
                var draft = await AdmissionTestHelpers.CreateDraftAsync(
                    client, context, childId, context.Slots[0]);
                var draftId = draft.GetProperty("id").GetGuid();

                var submit = await client.PostAsJsonAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{draftId}/submit",
                    AdmissionTestHelpers.SubmitConsentBody);
                Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);

                var json = await submit.Content.ReadFromJsonAsync<JsonElement>();
                Assert.False(json.GetProperty("succeeded").GetBoolean());
                Assert.Contains(
                    AdmissionErrorCodes.RequirementsIncomplete,
                    ReadCodes(json));
                Assert.True(json.TryGetProperty("data", out var data));
                Assert.True(data.TryGetProperty("missingRequirements", out var missing));
                Assert.True(missing.GetArrayLength() > 0);
            }
            finally
            {
                requirement.Deactivate(ownerId);
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task GetApplication_DetailNeverExposesStorageKey()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AdmissionTestHelpers.LoginWithRetryAsync(
            client,
            AuthTestHelpers.ParentEmail,
            AuthTestHelpers.DefaultPassword);
        var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
        var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
        var draft = await AdmissionTestHelpers.CreateDraftAsync(
            client, context, childId, context.Slots[0]);
        var draftId = draft.GetProperty("id").GetGuid();

        var detailResponse = await client.GetAsync($"{AdmissionTestHelpers.ApplicationsPath}/{draftId}");
        detailResponse.EnsureSuccessStatusCode();
        var raw = await detailResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("storageKey", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StorageKey", raw, StringComparison.Ordinal);
    }

    private static SchoolAdmissionRequirement CreateRequirement(
        Guid schoolId,
        string code,
        Guid? yearId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        int sortOrder,
        Guid userId)
    {
        var requirement = new SchoolAdmissionRequirement(
            schoolId,
            code,
            AdmissionRequirementKind.ApplicationDocument,
            "AR",
            "EN",
            null,
            null,
            true,
            sortOrder,
            branchId,
            stageId,
            gradeId,
            yearId,
            null,
            AdmissionRequiredDocumentCode.BirthCertificate,
            ".pdf",
            5_000_000,
            true,
            userId);
        requirement.Publish(userId);
        return requirement;
    }

    private static async Task<Guid> GetDemoSchoolIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/school-portal/schools");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in json.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("slug").GetString() == AuthTestHelpers.DemoSchoolSlug)
            {
                return item.GetProperty("id").GetGuid();
            }
        }

        throw new InvalidOperationException("Demo school not found in portal list.");
    }

    private static IReadOnlyList<string> ReadCodes(JsonElement json) =>
        json.GetProperty("errorCodes").EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
}
