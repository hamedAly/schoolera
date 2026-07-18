using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Schools.Fees;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

public sealed class FeeDisplayPolicyTests
{
    [Theory]
    [InlineData(null, false, true)]
    [InlineData(null, true, true)]
    [InlineData(FeeVisibilityPolicy.Public, false, true)]
    [InlineData(FeeVisibilityPolicy.Public, true, true)]
    [InlineData(FeeVisibilityPolicy.AuthenticatedParentsOnly, false, false)]
    [InlineData(FeeVisibilityPolicy.AuthenticatedParentsOnly, true, true)]
    public void FeeVisibilityResolver_ResolvesExpectedReveal(
        FeeVisibilityPolicy? policy,
        bool isAuthenticatedParent,
        bool expected)
    {
        Assert.Equal(
            expected,
            FeeVisibilityResolver.CanRevealDetailedFees(policy, isAuthenticatedParent));
    }

    [Fact]
    public void TuitionFee_RejectsNegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TuitionFee(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                Guid.NewGuid(),
                FeeCategory.Tuition,
                "EGP",
                -1m));
    }

    [Fact]
    public void Installment_RejectsInvalidPercentageBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SchoolFeeInstallmentDisplay(
                Guid.NewGuid(),
                1,
                "قسط",
                "Installment",
                FeeInstallmentAmountMode.Percentage,
                fixedAmount: null,
                percentage: 0m,
                dueDateUtc: null,
                dueWindowStartUtc: null,
                dueWindowEndUtc: null,
                notesAr: null,
                notesEn: null,
                sortOrder: 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SchoolFeeInstallmentDisplay.ValidateAmount(
                FeeInstallmentAmountMode.Percentage,
                null,
                101m));
    }

    [Fact]
    public void Discount_RejectsInvalidPercentageBounds()
    {
        var start = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SchoolPublishedDiscount.Validate(
                FeeDiscountType.Percentage,
                0m,
                null,
                start,
                start.AddDays(1)));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SchoolPublishedDiscount.Validate(
                FeeDiscountType.Percentage,
                150m,
                null,
                start,
                start.AddDays(1)));
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class FeeDisplayPolicyIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public FeeDisplayPolicyIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicProfile_Anonymous_OmitsAmounts_WhenAuthenticatedParentsOnly()
    {
        FeeVisibilityPolicy? original = null;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            var school = await EnsureDemoFeesPublishedAsync(db);
            original = school.FeeVisibilityPolicy;
            school.SetFeeVisibilityPolicy(FeeVisibilityPolicy.AuthenticatedParentsOnly);
            await db.SaveChangesAsync();
        }

        try
        {
            using var anon = _factory.CreateClient();
            var response = await anon.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var data = json.GetProperty("data");
            Assert.True(data.GetProperty("hasPublishedFees").GetBoolean());
            Assert.True(data.GetProperty("feesRequireLogin").GetBoolean());

            foreach (var fee in data.GetProperty("fees").EnumerateArray())
            {
                Assert.Equal(JsonValueKind.Null, fee.GetProperty("amount").ValueKind);
            }
        }
        finally
        {
            await RestoreFeeVisibilityAsync(original);
        }
    }

    [Fact]
    public async Task PublicProfile_Parent_SeesAmounts_WhenAuthenticatedParentsOnly()
    {
        FeeVisibilityPolicy? original = null;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            var school = await EnsureDemoFeesPublishedAsync(db);
            original = school.FeeVisibilityPolicy;
            school.SetFeeVisibilityPolicy(FeeVisibilityPolicy.AuthenticatedParentsOnly);
            await db.SaveChangesAsync();
        }

        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            await AuthTestHelpers.TryLoginAsync(
                client,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);

            var response = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var data = json.GetProperty("data");
            Assert.False(data.GetProperty("feesRequireLogin").GetBoolean());

            var fees = data.GetProperty("fees").EnumerateArray().ToArray();
            Assert.NotEmpty(fees);
            Assert.Equal(JsonValueKind.Number, fees[0].GetProperty("amount").ValueKind);
        }
        finally
        {
            await RestoreFeeVisibilityAsync(original);
        }
    }

    [Fact]
    public async Task FinanceOfficer_CanListTuitionFees()
    {
        var schoolId = await GetDemoSchoolIdAsync();

        using var finance = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            finance,
            AuthTestHelpers.FinanceOfficerEmail,
            AuthTestHelpers.DefaultPassword);

        var feesResponse = await finance.GetAsync($"/api/school-portal/schools/{schoolId}/tuition-fees");
        Assert.Equal(HttpStatusCode.OK, feesResponse.StatusCode);
        var feesJson = await feesResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(feesJson.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task AdmissionOfficer_DeniedManageFees_Publish()
    {
        var schoolId = await GetDemoSchoolIdAsync();
        Guid feeId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            await EnsureDemoFeesPublishedAsync(db);
            feeId = await db.TuitionFees
                .Where(fee => fee.SchoolBranch.SchoolId == schoolId)
                .Select(fee => fee.Id)
                .FirstAsync();
        }

        using var admission = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            admission,
            AuthTestHelpers.AdmissionOfficerEmail,
            AuthTestHelpers.DefaultPassword);

        var publish = await admission.PostAsync(
            $"/api/school-portal/schools/{schoolId}/tuition-fees/{feeId}/publish",
            null);
        Assert.Equal(HttpStatusCode.Forbidden, publish.StatusCode);

        var publishJson = await publish.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            "schoolPortal.accessDenied",
            publishJson.GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString()));
    }

    private async Task<Guid> GetDemoSchoolIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        return await db.Schools
            .Where(school => school.Slug == AuthTestHelpers.DemoSchoolSlug)
            .Select(school => school.Id)
            .SingleAsync();
    }

    private async Task RestoreFeeVisibilityAsync(FeeVisibilityPolicy? original)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var school = await db.Schools.FirstAsync(s => s.Slug == AuthTestHelpers.DemoSchoolSlug);
        school.SetFeeVisibilityPolicy(original);
        await db.SaveChangesAsync();
    }

    private static async Task<School> EnsureDemoFeesPublishedAsync(SchooleraDbContext db)
    {
        var school = await db.Schools
            .Include(s => s.Branches)
                .ThenInclude(b => b.TuitionFees)
            .FirstAsync(s => s.Slug == AuthTestHelpers.DemoSchoolSlug);

        var fees = school.Branches.SelectMany(b => b.TuitionFees).ToList();
        if (fees.Count == 0)
        {
            var branch = school.Branches.FirstOrDefault(b => b.IsActive)
                ?? school.Branches.First();
            var stageId = await db.EducationalStages
                .Where(stage => stage.IsActive)
                .OrderBy(stage => stage.SortOrder)
                .Select(stage => stage.Id)
                .FirstAsync();
            var yearId = await db.AcademicYears
                .Where(year => year.IsActive)
                .OrderByDescending(year => year.IsCurrent)
                .Select(year => year.Id)
                .FirstAsync();

            var fee = new TuitionFee(
                branch.Id,
                stageId,
                null,
                yearId,
                FeeCategory.Tuition,
                "EGP",
                25000m);
            fee.Publish();
            db.TuitionFees.Add(fee);
            await db.SaveChangesAsync();

            school = await db.Schools
                .Include(s => s.Branches)
                    .ThenInclude(b => b.TuitionFees)
                .FirstAsync(s => s.Slug == AuthTestHelpers.DemoSchoolSlug);
            fees = school.Branches.SelectMany(b => b.TuitionFees).ToList();
        }

        foreach (var fee in fees)
        {
            if (!fee.IsActive)
            {
                fee.Activate();
            }

            if (!fee.IsPublished)
            {
                fee.Publish();
            }
        }

        await db.SaveChangesAsync();
        return school;
    }
}