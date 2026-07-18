using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Admissions.Common;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class ChildAgeEligibilityTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public ChildAgeEligibilityTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(2020, 1, 15, 2023, 1, 14, 35)]
    [InlineData(2020, 1, 15, 2023, 1, 15, 36)]
    [InlineData(2020, 1, 15, 2023, 1, 16, 36)]
    public void CompletedMonths_AnniversaryDay(
        int by, int bm, int bd, int ry, int rm, int rd, int expected)
    {
        var months = CompletedCalendarMonths.Compute(
            new DateOnly(by, bm, bd),
            new DateOnly(ry, rm, rd));
        Assert.Equal(expected, months);
    }

    [Fact]
    public void CompletedMonths_MonthEnd_31_To_30()
    {
        Assert.Equal(1, CompletedCalendarMonths.Compute(
            new DateOnly(2020, 1, 31),
            new DateOnly(2020, 3, 30)));
        Assert.Equal(2, CompletedCalendarMonths.Compute(
            new DateOnly(2020, 1, 31),
            new DateOnly(2020, 3, 31)));
    }

    [Fact]
    public void CompletedMonths_Feb29_NonLeap_UsesFeb28()
    {
        Assert.True(CompletedCalendarMonths.TryCompute(
            new DateOnly(2020, 2, 29),
            new DateOnly(2021, 2, 28),
            out var months));
        Assert.Equal(12, months);

        Assert.Equal(11, CompletedCalendarMonths.Compute(
            new DateOnly(2020, 2, 29),
            new DateOnly(2021, 2, 27)));
    }

    [Fact]
    public void CompletedMonths_LeapFeb29()
    {
        Assert.Equal(48, CompletedCalendarMonths.Compute(
            new DateOnly(2020, 2, 29),
            new DateOnly(2024, 2, 29)));
    }

    [Fact]
    public void CompletedMonths_BirthAfterReference_TryComputeFalse()
    {
        Assert.False(CompletedCalendarMonths.TryCompute(
            new DateOnly(2024, 1, 1),
            new DateOnly(2023, 1, 1),
            out _));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CompletedCalendarMonths.Compute(
                new DateOnly(2024, 1, 1),
                new DateOnly(2023, 1, 1)));
    }

    [Fact]
    public void ResolveApplicable_PicksMostSpecificPublishedRule()
    {
        var schoolId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var stageYear = CreateRule(schoolId, userId, stageId, yearId);
        var branchStageYear = CreateRule(schoolId, userId, stageId, yearId, branchId: branchId);
        var branchGradeYear = CreateRule(schoolId, userId, stageId, yearId, branchId: branchId, gradeId: gradeId);

        stageYear.Publish(userId);
        branchStageYear.Publish(userId);
        branchGradeYear.Publish(userId);

        var applicable = ChildAgeEligibilityCatalog.ResolveApplicable(
            [stageYear, branchStageYear, branchGradeYear],
            branchId,
            stageId,
            gradeId,
            yearId);

        Assert.NotNull(applicable);
        Assert.Equal(branchGradeYear.Id, applicable!.Id);
    }

    [Fact]
    public async Task SchoolPortalRules_Parent_Returns403()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword);

        var profile = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        profile.EnsureSuccessStatusCode();
        var schoolId = (await profile.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/age-eligibility-rules");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_WithoutCsrf_Returns400()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(
            client,
            AuthTestHelpers.SchoolOwnerEmail,
            AuthTestHelpers.DefaultPassword);

        var profile = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        profile.EnsureSuccessStatusCode();
        var schoolId = (await profile.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        Guid stageId;
        Guid yearId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            stageId = await db.EducationalStages.Select(s => s.Id).FirstAsync();
            yearId = await db.AcademicYears.Where(y => y.IsActive).Select(y => y.Id).FirstAsync();
        }

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/age-eligibility-rules",
            new
            {
                minAgeCompletedMonths = 36,
                maxAgeCompletedMonths = 72,
                referenceDateMode = (int)ChildAgeReferenceDateMode.AcademicYearStart,
                manualExceptionAllowed = true,
                educationalStageId = stageId,
                academicYearId = yearId,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RuleNotConfigured_AllowsDraftCreate()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            Assert.Equal((int)AdmissionApplicationStatus.Draft, draft.GetProperty("status").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Submit_BlockedWhenBelowMinWithoutException()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var slot = context.Slots[0];

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                var rule = new SchoolChildAgeEligibilityRule(
                    context.SchoolId,
                    slot.EducationalStageId,
                    context.AcademicYearId,
                    minAgeCompletedMonths: 300,
                    maxAgeCompletedMonths: 300,
                    ChildAgeReferenceDateMode.AcademicYearStart,
                    explanationAr: "عمر",
                    explanationEn: "Age",
                    manualExceptionAllowed: false,
                    schoolBranchId: null,
                    gradeId: null,
                    createdByUserId: Guid.NewGuid());
                rule.Publish(Guid.NewGuid());
                db.SchoolChildAgeEligibilityRules.Add(rule);
                await db.SaveChangesAsync();
            }

            var childId = await AdmissionTestHelpers.CreateChildAsync(client, slot.GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                slot);
            var appId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{appId}/submit",
                AdmissionTestHelpers.SubmitConsentBody);

            Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);
            var submitJson = await submit.Content.ReadFromJsonAsync<JsonElement>();
            var codes = submitJson.GetProperty("errorCodes").EnumerateArray()
                .Select(c => c.GetString())
                .ToArray();
            Assert.Contains("admission.application.ageNotEligible", codes);
        }
        finally
        {
            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.SchoolChildAgeEligibilityRules
                    .Where(r => r.School.Slug == AuthTestHelpers.DemoSchoolSlug)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(r => r.IsActive, false)
                        .SetProperty(r => r.PublicationStatus, ChildAgeEligibilityPublicationStatus.Draft));
            }

            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    private static SchoolChildAgeEligibilityRule CreateRule(
        Guid schoolId,
        Guid userId,
        Guid stageId,
        Guid yearId,
        Guid? branchId = null,
        Guid? gradeId = null) =>
        new(
            schoolId,
            stageId,
            yearId,
            minAgeCompletedMonths: 36,
            maxAgeCompletedMonths: 72,
            ChildAgeReferenceDateMode.AcademicYearStart,
            explanationAr: null,
            explanationEn: null,
            manualExceptionAllowed: true,
            branchId,
            gradeId,
            userId);
}
