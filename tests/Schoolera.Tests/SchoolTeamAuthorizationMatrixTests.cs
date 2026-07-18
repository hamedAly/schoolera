using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

/// <summary>
/// Prompt 8 authorization matrix: role × school-portal endpoint categories.
/// Uses seeded Owner / SchoolAdmin / AdmissionOfficer / FinanceOfficer / ContentModerator.
/// </summary>
[Collection(WebApplicationFactoryCollection.Name)]
public sealed class SchoolTeamAuthorizationMatrixTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    private static readonly object ProfilePutBody = new
    {
        nameAr = "مدرسة",
        nameEn = "School",
        shortDescriptionAr = (string?)null,
        shortDescriptionEn = (string?)null,
        fullDescriptionAr = (string?)null,
        fullDescriptionEn = (string?)null,
        schoolType = 2,
        genderType = 3,
        foundedYear = (int?)null,
        studentCount = (int?)null,
        publicPhone = (string?)null,
        publicEmail = (string?)null,
        websiteUrl = (string?)null,
        whatsAppNumber = (string?)null,
        seoTitleAr = (string?)null,
        seoTitleEn = (string?)null,
        seoDescriptionAr = (string?)null,
        seoDescriptionEn = (string?)null,
    };

    public SchoolTeamAuthorizationMatrixTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTeam_WhenAnonymous_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/school-portal/schools/{Guid.NewGuid()}/team");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthTestHelpers.ParentEmail, HttpStatusCode.Forbidden)]
    [InlineData(AuthTestHelpers.PlatformAdminEmail, HttpStatusCode.Forbidden)]
    [InlineData(AuthTestHelpers.SupportAgentEmail, HttpStatusCode.Forbidden)]
    public async Task GetTeam_WhenNonPortalGlobalRole_Returns403(string email, HttpStatusCode expected)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync($"/api/school-portal/schools/{Guid.NewGuid()}/team");
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthTestHelpers.SchoolOwnerEmail, HttpStatusCode.OK, null)]
    [InlineData(AuthTestHelpers.SchoolAdminEmail, HttpStatusCode.OK, null)]
    [InlineData(AuthTestHelpers.AdmissionOfficerEmail, HttpStatusCode.Forbidden, SchoolPortalErrorCodes.AccessDenied)]
    [InlineData(AuthTestHelpers.FinanceOfficerEmail, HttpStatusCode.Forbidden, SchoolPortalErrorCodes.AccessDenied)]
    [InlineData(AuthTestHelpers.ContentModeratorEmail, HttpStatusCode.Forbidden, SchoolPortalErrorCodes.AccessDenied)]
    public async Task GetTeam_RoleMatrix(
        string email,
        HttpStatusCode expectedStatus,
        string? expectedErrorCode)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.GetAsync($"/api/school-portal/schools/{schoolId}/team");
        Assert.Equal(expectedStatus, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (expectedStatus == HttpStatusCode.OK)
        {
            Assert.True(json.GetProperty("succeeded").GetBoolean());
        }
        else if (expectedErrorCode is not null)
        {
            Assert.Contains(expectedErrorCode, ReadErrorCodes(json));
        }
    }

    [Theory]
    [InlineData(AuthTestHelpers.SchoolAdminEmail, SchoolPortalErrorCodes.OwnerRequired)]
    [InlineData(AuthTestHelpers.AdmissionOfficerEmail, SchoolPortalErrorCodes.OwnerRequired)]
    [InlineData(AuthTestHelpers.FinanceOfficerEmail, SchoolPortalErrorCodes.OwnerRequired)]
    [InlineData(AuthTestHelpers.ContentModeratorEmail, SchoolPortalErrorCodes.OwnerRequired)]
    public async Task PostTeamMembers_NonOwner_Returns403(string email, string expectedErrorCode)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/members",
            new
            {
                email = AuthTestHelpers.SchoolAdminEmail,
                role = (int)SchoolTeamRole.SchoolAdmin,
                branchScopeMode = (int)SchoolBranchScopeMode.AllBranches,
                branchIds = (Guid[]?)null,
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(expectedErrorCode, ReadErrorCodes(json));
    }

    [Fact]
    public async Task PostTeamMembers_WhenOwner_PassesAuthorization()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/members",
            new
            {
                email = AuthTestHelpers.SchoolAdminEmail,
                role = (int)SchoolTeamRole.SchoolAdmin,
                branchScopeMode = (int)SchoolBranchScopeMode.AllBranches,
                branchIds = (Guid[]?)null,
            });

        // Seeded SchoolAdmin already has an active membership → AlreadyExists (auth still Owner-only).
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(json.GetProperty("succeeded").GetBoolean());
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(SchoolPortalErrorCodes.TeamMemberAlreadyExists, ReadErrorCodes(json));
        }
    }

    [Fact]
    public async Task PostTeamMembers_WhenCsrfMissing_Returns400()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/members",
            new
            {
                email = AuthTestHelpers.SchoolAdminEmail,
                role = (int)SchoolTeamRole.SchoolAdmin,
                branchScopeMode = (int)SchoolBranchScopeMode.AllBranches,
                branchIds = (Guid[]?)null,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthTestHelpers.SchoolAdminEmail, SchoolPortalErrorCodes.OwnerRequired)]
    [InlineData(AuthTestHelpers.AdmissionOfficerEmail, SchoolPortalErrorCodes.OwnerRequired)]
    [InlineData(AuthTestHelpers.FinanceOfficerEmail, SchoolPortalErrorCodes.OwnerRequired)]
    public async Task TransferOwnership_NonOwner_Returns403(string email, string expectedErrorCode)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/transfer-ownership",
            new { newOwnerEmail = AuthTestHelpers.SchoolOwnerEmail });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(expectedErrorCode, ReadErrorCodes(json));
    }

    [Fact]
    public async Task TransferOwnership_WhenOwner_InvalidTarget_ReturnsStableCodes()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var missing = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/transfer-ownership",
            new { newOwnerEmail = "nobody@schoolera.local" });
        // UserNotFound maps to 400 in ApiControllerBase (not listed under 404).
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var missingJson = await missing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(SchoolPortalErrorCodes.UserNotFound, ReadErrorCodes(missingJson));

        var parent = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/transfer-ownership",
            new { newOwnerEmail = AuthTestHelpers.ParentEmail });
        Assert.Equal(HttpStatusCode.BadRequest, parent.StatusCode);
        var parentJson = await parent.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(SchoolPortalErrorCodes.OwnershipTransferInvalid, ReadErrorCodes(parentJson));

        var adminOnly = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/transfer-ownership",
            new { newOwnerEmail = AuthTestHelpers.SchoolAdminEmail });
        Assert.Equal(HttpStatusCode.BadRequest, adminOnly.StatusCode);
        var adminJson = await adminOnly.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(SchoolPortalErrorCodes.OwnershipTransferInvalid, ReadErrorCodes(adminJson));

        // Transfer to self is a no-op success (stable owner row).
        var self = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/team/transfer-ownership",
            new { newOwnerEmail = AuthTestHelpers.SchoolOwnerEmail });
        Assert.Equal(HttpStatusCode.OK, self.StatusCode);
    }

    [Theory]
    [InlineData(AuthTestHelpers.SchoolOwnerEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.SchoolAdminEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.ContentModeratorEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.AdmissionOfficerEmail, HttpStatusCode.Forbidden)]
    [InlineData(AuthTestHelpers.FinanceOfficerEmail, HttpStatusCode.Forbidden)]
    public async Task GetProfile_RoleMatrix(string email, HttpStatusCode expectedStatus)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.GetAsync($"/api/school-portal/schools/{schoolId}/profile");
        Assert.Equal(expectedStatus, response.StatusCode);

        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(SchoolPortalErrorCodes.AccessDenied, ReadErrorCodes(json));
        }
    }

    [Theory]
    [InlineData(AuthTestHelpers.AdmissionOfficerEmail)]
    [InlineData(AuthTestHelpers.FinanceOfficerEmail)]
    public async Task PutProfile_WhenAdmissionOrFinance_Returns403AccessDenied(string email)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.PutAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/profile",
            ProfilePutBody);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(SchoolPortalErrorCodes.AccessDenied, ReadErrorCodes(json));
    }

    [Theory]
    [InlineData(AuthTestHelpers.SchoolOwnerEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.SchoolAdminEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.FinanceOfficerEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.AdmissionOfficerEmail, HttpStatusCode.Forbidden)]
    [InlineData(AuthTestHelpers.ContentModeratorEmail, HttpStatusCode.Forbidden)]
    public async Task GetTuitionFees_RoleMatrix(string email, HttpStatusCode expectedStatus)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.GetAsync($"/api/school-portal/schools/{schoolId}/tuition-fees");
        Assert.Equal(expectedStatus, response.StatusCode);

        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(SchoolPortalErrorCodes.AccessDenied, ReadErrorCodes(json));
        }
    }

    [Theory]
    [InlineData(AuthTestHelpers.SchoolOwnerEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.SchoolAdminEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.AdmissionOfficerEmail, HttpStatusCode.OK)]
    [InlineData(AuthTestHelpers.FinanceOfficerEmail, HttpStatusCode.Forbidden)]
    [InlineData(AuthTestHelpers.ContentModeratorEmail, HttpStatusCode.Forbidden)]
    public async Task GetApplications_RoleMatrix(string email, HttpStatusCode expectedStatus)
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));
        var schoolId = await TryGetDemoSchoolIdAsync(client);
        Assert.NotNull(schoolId);

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/applications?pageNumber=1&pageSize=5");
        Assert.Equal(expectedStatus, response.StatusCode);

        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(SchoolPortalErrorCodes.AccessDenied, ReadErrorCodes(json));
        }
    }

    [Fact]
    public async Task ListSchools_PermissionDto_MatchesRoleFlags()
    {
        await AssertPermissionsAsync(
            AuthTestHelpers.SchoolOwnerEmail,
            canManageTeam: true,
            canTransferOwnership: true,
            canViewApplications: true,
            canViewFees: true,
            canManageContent: true,
            isOwner: true);

        await AssertPermissionsAsync(
            AuthTestHelpers.SchoolAdminEmail,
            canManageTeam: false,
            canTransferOwnership: false,
            canViewApplications: true,
            canViewFees: true,
            canManageContent: true,
            isOwner: false);

        await AssertPermissionsAsync(
            AuthTestHelpers.AdmissionOfficerEmail,
            canManageTeam: false,
            canTransferOwnership: false,
            canViewApplications: true,
            canViewFees: false,
            canManageContent: false,
            isOwner: false);

        await AssertPermissionsAsync(
            AuthTestHelpers.FinanceOfficerEmail,
            canManageTeam: false,
            canTransferOwnership: false,
            canViewApplications: false,
            canViewFees: true,
            canManageContent: false,
            isOwner: false);

        await AssertPermissionsAsync(
            AuthTestHelpers.ContentModeratorEmail,
            canManageTeam: false,
            canTransferOwnership: false,
            canViewApplications: false,
            canViewFees: false,
            canManageContent: true,
            isOwner: false);
    }

    [Fact]
    public async Task GetProfile_WhenOtherSchool_Returns404SchoolNotFound()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolAdminEmail,
            AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync($"/api/school-portal/schools/{Guid.NewGuid()}/profile");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(SchoolPortalErrorCodes.SchoolNotFound, ReadErrorCodes(json));
    }

    [Fact]
    public async Task GetProfile_WhenMembershipInactive_Returns404SchoolNotFound()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                client,
                AuthTestHelpers.ContentModeratorEmail,
                AuthTestHelpers.DefaultPassword));
            var schoolId = await TryGetDemoSchoolIdAsync(client);
            Assert.NotNull(schoolId);

            await SetMembershipActiveAsync(
                AuthTestHelpers.ContentModeratorEmail,
                schoolId.Value,
                isActive: false);

            try
            {
                var response = await client.GetAsync($"/api/school-portal/schools/{schoolId}/profile");
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Contains(SchoolPortalErrorCodes.SchoolNotFound, ReadErrorCodes(json));
            }
            finally
            {
                await SetMembershipActiveAsync(
                    AuthTestHelpers.ContentModeratorEmail,
                    schoolId.Value,
                    isActive: true);
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task StartReview_WhenAdmissionOfficer_WrongBranchScope_ReturnsBranchScopeDenied()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

            var parentClient = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, parentClient);
            var childId = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await parentClient.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.True(
                submit.StatusCode == HttpStatusCode.OK,
                $"Submit failed: {await submit.Content.ReadAsStringAsync()}");

            using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                ownerClient,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword));
            var schoolId = await TryGetDemoSchoolIdAsync(ownerClient);
            Assert.NotNull(schoolId);

            var applicationBranchId = await GetApplicationBranchIdAsync(applicationId);
            Assert.NotEqual(Guid.Empty, applicationBranchId);

            var otherBranchId = await EnsureOtherBranchAsync(schoolId.Value, applicationBranchId);
            Assert.NotEqual(applicationBranchId, otherBranchId);

            // Scope officer to a different school branch so the application branch is denied.
            await SetOfficerBranchScopeAsync(
                AuthTestHelpers.AdmissionOfficerEmail,
                schoolId.Value,
                SchoolBranchScopeMode.SelectedBranches,
                [otherBranchId]);

            try
            {
                using var officerClient = AuthTestHelpers.CreateCookieClient(_factory);
                Assert.True(await AuthTestHelpers.TryLoginAsync(
                    officerClient,
                    AuthTestHelpers.AdmissionOfficerEmail,
                    AuthTestHelpers.DefaultPassword));

                // In-scope list still allowed at school shell; start-review checks application branch.
                var list = await officerClient.GetAsync(
                    $"/api/school-portal/schools/{schoolId}/applications?pageNumber=1&pageSize=5");
                Assert.Equal(HttpStatusCode.OK, list.StatusCode);

                var start = await officerClient.PostAsJsonAsync(
                    $"/api/school-portal/schools/{schoolId}/applications/{applicationId}/start-review",
                    new { });
                Assert.Equal(HttpStatusCode.Forbidden, start.StatusCode);
                var startJson = await start.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Contains(SchoolPortalErrorCodes.BranchScopeDenied, ReadErrorCodes(startJson));
            }
            finally
            {
                await SetOfficerBranchScopeAsync(
                    AuthTestHelpers.AdmissionOfficerEmail,
                    schoolId.Value,
                    SchoolBranchScopeMode.AllBranches,
                    Array.Empty<Guid>());
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task DownloadAttachment_WhenAdmissionOfficer_InScope_Allowed()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

            var parentClient = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, parentClient);
            var childId = await AdmissionTestHelpers.CreateChildAsync(parentClient, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                parentClient,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            Guid attachmentId;
            using (var uploadContent = AdmissionTestHelpers.BuildPdfUploadContent(
                       AdmissionTestHelpers.MinimalPdfBytes))
            {
                var upload = await parentClient.PostAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                    uploadContent);
                Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
                var uploaded = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
                attachmentId = uploaded.GetProperty("attachments").EnumerateArray().First()
                    .GetProperty("id")
                    .GetGuid();
            }

            using var officerClient = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                officerClient,
                AuthTestHelpers.AdmissionOfficerEmail,
                AuthTestHelpers.DefaultPassword));
            var schoolId = await TryGetDemoSchoolIdAsync(officerClient);
            Assert.NotNull(schoolId);

            var download = await officerClient.GetAsync(
                $"/api/school-portal/schools/{schoolId}/applications/{applicationId}/attachments/{attachmentId}/download");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    private async Task AssertPermissionsAsync(
        string email,
        bool canManageTeam,
        bool canTransferOwnership,
        bool canViewApplications,
        bool canViewFees,
        bool canManageContent,
        bool isOwner)
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(client, email, AuthTestHelpers.DefaultPassword));

        var response = await client.GetAsync("/api/school-portal/schools");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());

        var demo = json.GetProperty("data").EnumerateArray()
            .FirstOrDefault(item => item.GetProperty("slug").GetString() == AuthTestHelpers.DemoSchoolSlug);
        Assert.NotEqual(default, demo);

        var permissions = demo.GetProperty("permissions");
        Assert.Equal(isOwner, demo.GetProperty("isOwner").GetBoolean());
        Assert.Equal(canManageTeam, permissions.GetProperty("canManageTeam").GetBoolean());
        Assert.Equal(canTransferOwnership, permissions.GetProperty("canTransferOwnership").GetBoolean());
        Assert.Equal(canViewApplications, permissions.GetProperty("canViewApplications").GetBoolean());
        Assert.Equal(canViewFees, permissions.GetProperty("canViewFees").GetBoolean());
        Assert.Equal(canManageContent, permissions.GetProperty("canManageContent").GetBoolean());
    }

    private async Task SetMembershipActiveAsync(string email, Guid schoolId, bool isActive)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var normalized = email.ToUpperInvariant();
        var user = await db.Users.FirstAsync(entry => entry.NormalizedEmail == normalized);
        var member = await db.SchoolTeamMembers.FirstAsync(
            entry => entry.SchoolId == schoolId && entry.UserId == user.Id);

        if (isActive)
        {
            member.Reactivate();
        }
        else
        {
            member.Deactivate();
        }

        await db.SaveChangesAsync();
    }

    private async Task SetOfficerBranchScopeAsync(
        string email,
        Guid schoolId,
        SchoolBranchScopeMode mode,
        IReadOnlyList<Guid> branchIds)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var normalized = email.ToUpperInvariant();
        var user = await db.Users.FirstAsync(entry => entry.NormalizedEmail == normalized);
        var member = await db.SchoolTeamMembers
            .Include(entry => entry.BranchAssignments)
            .FirstAsync(entry => entry.SchoolId == schoolId && entry.UserId == user.Id && entry.IsActive);

        member.SetBranchScope(mode, branchIds);
        await db.SaveChangesAsync();
    }

    private async Task<Guid> EnsureOtherBranchAsync(Guid schoolId, Guid applicationBranchId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();

        var other = await db.SchoolBranches
            .AsNoTracking()
            .Where(branch => branch.SchoolId == schoolId && branch.Id != applicationBranchId)
            .Select(branch => branch.Id)
            .FirstOrDefaultAsync();

        if (other != Guid.Empty)
        {
            return other;
        }

        var city = await db.Cities.AsNoTracking().OrderBy(item => item.SortOrder).FirstAsync();
        var district = await db.Districts.AsNoTracking()
            .Where(item => item.CityId == city.Id)
            .OrderBy(item => item.SortOrder)
            .FirstAsync();

        var branch = new Domain.Entities.SchoolBranch(
            schoolId,
            "فرع نطاق الاختبار",
            "Branch Scope Test",
            $"scope-test-{Guid.NewGuid():N}"[..32],
            city.Id,
            district.Id,
            isMainBranch: false);
        db.SchoolBranches.Add(branch);
        await db.SaveChangesAsync();
        return branch.Id;
    }

    private async Task<Guid> GetApplicationBranchIdAsync(Guid applicationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        return await db.AdmissionApplications
            .AsNoTracking()
            .Where(entry => entry.Id == applicationId)
            .Select(entry => entry.SchoolBranchId)
            .FirstAsync();
    }

    private static async Task<Guid?> TryGetDemoSchoolIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/school-portal/schools");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!json.GetProperty("succeeded").GetBoolean())
        {
            return null;
        }

        foreach (var item in json.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("slug").GetString() == AuthTestHelpers.DemoSchoolSlug)
            {
                return item.GetProperty("id").GetGuid();
            }
        }

        return null;
    }

    private static IEnumerable<string> ReadErrorCodes(JsonElement json) =>
        json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()!);
}
