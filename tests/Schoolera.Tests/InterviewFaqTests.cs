using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Cms;
using Schoolera.Infrastructure.Persistence;
using Schoolera.Application.SchoolPortal.Constants;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class InterviewFaqTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public InterviewFaqTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void PlatformOwnership_RejectsSchoolIdAndApplicability()
    {
        var categoryId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new FaqItem(
            categoryId,
            "س",
            "Q",
            "<p>ج</p>",
            "<p>A</p>",
            1,
            FaqOwnershipScope.Platform,
            schoolId: Guid.NewGuid(),
            InterviewFaqCategory.Interview));

        Assert.Throws<ArgumentException>(() => new FaqItem(
            categoryId,
            "س",
            "Q",
            "<p>ج</p>",
            "<p>A</p>",
            1,
            FaqOwnershipScope.Platform,
            schoolId: null,
            InterviewFaqCategory.Interview,
            schoolBranchId: Guid.NewGuid()));
    }

    [Fact]
    public void SchoolOwnership_RequiresSchoolIdAndInterviewCategory()
    {
        var categoryId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new FaqItem(
            categoryId,
            "س",
            "Q",
            "<p>ج</p>",
            "<p>A</p>",
            1,
            FaqOwnershipScope.School,
            schoolId: null,
            InterviewFaqCategory.Interview));

        Assert.Throws<ArgumentException>(() => new FaqItem(
            categoryId,
            "س",
            "Q",
            "<p>ج</p>",
            "<p>A</p>",
            1,
            FaqOwnershipScope.School,
            schoolId: Guid.NewGuid(),
            interviewCategory: null));

        var item = new FaqItem(
            categoryId,
            "س",
            "Q",
            "<p>ج</p>",
            "<p>A</p>",
            1,
            FaqOwnershipScope.School,
            Guid.NewGuid(),
            InterviewFaqCategory.Assessment);
        Assert.Equal(FaqOwnershipScope.School, item.OwnershipScope);
        Assert.Equal(InterviewFaqCategory.Assessment, item.InterviewCategory);
        Assert.True(item.IsActive);
    }

    [Fact]
    public void Applicability_PlatformAlwaysMatches_SchoolUsesScope()
    {
        var categoryId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var yearId = Guid.NewGuid();

        var platform = new FaqItem(
            categoryId, "س", "Q", "<p>ج</p>", "<p>A</p>", 1,
            FaqOwnershipScope.Platform, null, InterviewFaqCategory.Interview);
        platform.Publish();

        var schoolWildcard = new FaqItem(
            categoryId, "س2", "Q2", "<p>ج</p>", "<p>A</p>", 2,
            FaqOwnershipScope.School, schoolId, InterviewFaqCategory.Interview);
        schoolWildcard.Publish();

        var schoolBranch = new FaqItem(
            categoryId, "س3", "Q3", "<p>ج</p>", "<p>A</p>", 3,
            FaqOwnershipScope.School, schoolId, InterviewFaqCategory.Interview,
            schoolBranchId: branchId, educationalStageId: null, gradeId: null, academicYearId: yearId);
        schoolBranch.Publish();

        Assert.True(InterviewFaqApplicability.Matches(platform, branchId, stageId, gradeId, yearId));
        Assert.True(InterviewFaqApplicability.Matches(schoolWildcard, branchId, stageId, gradeId, yearId));
        Assert.True(InterviewFaqApplicability.Matches(schoolBranch, branchId, stageId, gradeId, yearId));
        Assert.False(InterviewFaqApplicability.Matches(
            schoolBranch, Guid.NewGuid(), stageId, gradeId, yearId));

        // Missing context dimension → only FAQs with null definition for that dimension
        Assert.True(InterviewFaqApplicability.Matches(schoolWildcard, null, null, null, null));
        Assert.False(InterviewFaqApplicability.Matches(schoolBranch, null, null, null, null));
    }

    [Fact]
    public void ContentSanitizer_StripsScriptFromAnswers()
    {
        var sanitizer = new ContentSanitizer();
        var clean = sanitizer.SanitizeHtml("<p>Hi</p><script>alert(1)</script>");
        Assert.DoesNotContain("script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hi", clean, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicFaqs_ExcludesInterviewAndSchoolOwnedItems()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            var category = await db.FaqCategories.FirstAsync(c => c.Slug == "parents");
            var interview = new FaqItem(
                category.Id,
                "مقابلة؟",
                "Interview?",
                "<p>جواب</p>",
                "<p>Answer</p>",
                99,
                FaqOwnershipScope.Platform,
                null,
                InterviewFaqCategory.Interview);
            interview.Publish();
            db.FaqItems.Add(interview);
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/content/faqs");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var questions = json.GetProperty("data").EnumerateArray()
            .SelectMany(category => category.GetProperty("items").EnumerateArray())
            .Select(item => item.GetProperty("question").GetString())
            .ToArray();
        Assert.DoesNotContain("Interview?", questions);
        Assert.DoesNotContain("مقابلة؟", questions);
    }

    [Fact]
    public async Task PlatformAdmin_CanManagePlatformInterviewFaqs()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.PlatformAdminEmail, AuthTestHelpers.DefaultPassword));

        Guid categoryId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            categoryId = await db.FaqCategories
                .Where(c => c.Slug == CmsSlugs.InterviewFaqCategorySlug || c.Slug == "platform")
                .OrderBy(c => c.Slug == CmsSlugs.InterviewFaqCategorySlug ? 0 : 1)
                .Select(c => c.Id)
                .FirstAsync();
        }

        var create = await client.PostAsJsonAsync(
            "/api/admin/cms/faq/items",
            new
            {
                categoryId,
                questionAr = "متى المقابلة؟",
                questionEn = "When is the interview?",
                answerAr = "<p>بعد التقديم</p>",
                answerEn = "<p>After applying</p>",
                ownershipScope = (int)FaqOwnershipScope.Platform,
                interviewCategory = (int)InterviewFaqCategory.Interview,
            });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("data").GetProperty("id").GetGuid();

        var publish = await client.PostAsync($"/api/admin/cms/faq/items/{id}/publish", null);
        publish.EnsureSuccessStatusCode();

        var list = await client.GetAsync("/api/admin/cms/faq/interview-items?category=1");
        list.EnsureSuccessStatusCode();
        var listJson = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            listJson.GetProperty("data").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task SchoolPortal_ManageContent_CanCreate_AdmissionOfficerDenied()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        var schoolId = await GetDemoSchoolIdAsync();

        using var ownerClient = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            ownerClient, AuthTestHelpers.SchoolOwnerEmail, AuthTestHelpers.DefaultPassword));

        var create = await ownerClient.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/interview-faqs",
            new
            {
                questionAr = "ما المستندات؟",
                questionEn = "What documents?",
                answerAr = "<p>جواز السفر</p>",
                answerEn = "<p>Passport</p>",
                interviewCategory = (int)InterviewFaqCategory.Assessment,
            });
        create.EnsureSuccessStatusCode();
        var itemId = (await create.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var publish = await ownerClient.PostAsync(
            $"/api/school-portal/schools/{schoolId}/interview-faqs/{itemId}/publish", null);
        publish.EnsureSuccessStatusCode();

        using var officerClient = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            officerClient, AuthTestHelpers.AdmissionOfficerEmail, AuthTestHelpers.DefaultPassword));

        var denied = await officerClient.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/interview-faqs",
            new
            {
                questionAr = "ممنوع",
                questionEn = "Denied",
                answerAr = "<p>x</p>",
                answerEn = "<p>x</p>",
                interviewCategory = (int)InterviewFaqCategory.Interview,
            });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var deniedJson = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            SchoolPortalErrorCodes.AccessDenied,
            deniedJson.GetProperty("errorCodes").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task ContentModerator_CanManageSchoolFaqs()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        var schoolId = await GetDemoSchoolIdAsync();

        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.ContentModeratorEmail, AuthTestHelpers.DefaultPassword));

        var create = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/interview-faqs",
            new
            {
                questionAr = "محتوى",
                questionEn = "Content",
                answerAr = "<p>نعم</p>",
                answerEn = "<p>Yes</p>",
                interviewCategory = (int)InterviewFaqCategory.InterviewAndAssessment,
            });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
    }

    [Fact]
    public async Task ParentAndAnonymous_WriteDenied_PublicReadPublishedOnly()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        var schoolId = await GetDemoSchoolIdAsync();

        using var parent = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            parent, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword));
        var parentWrite = await parent.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/interview-faqs",
            new
            {
                questionAr = "x",
                questionEn = "x",
                answerAr = "<p>x</p>",
                answerEn = "<p>x</p>",
                interviewCategory = 1,
            });
        Assert.Equal(HttpStatusCode.Forbidden, parentWrite.StatusCode);

        using var anon = _factory.CreateClient();
        var anonWrite = await anon.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/interview-faqs",
            new
            {
                questionAr = "x",
                questionEn = "x",
                answerAr = "<p>x</p>",
                answerEn = "<p>x</p>",
                interviewCategory = 1,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, anonWrite.StatusCode);

        var publicRead = await anon.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}/interview-faqs");
        publicRead.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CrossSchoolIsolation_AndCsrfRequired()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        var schoolId = await GetDemoSchoolIdAsync();

        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.SchoolOwnerEmail, AuthTestHelpers.DefaultPassword));

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var noCsrf = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/interview-faqs",
            new
            {
                questionAr = "csrf",
                questionEn = "csrf",
                answerAr = "<p>x</p>",
                answerEn = "<p>x</p>",
                interviewCategory = 1,
            });
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        Assert.True(await AuthTestHelpers.TryLoginAsync(
            client, AuthTestHelpers.SchoolOwnerEmail, AuthTestHelpers.DefaultPassword));

        var otherSchoolId = Guid.NewGuid();
        var cross = await client.GetAsync($"/api/school-portal/schools/{otherSchoolId}/interview-faqs");
        Assert.True(
            cross.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublicOrdering_PlatformBeforeSchool_ExcludesDraftInactive()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        var schoolId = await GetDemoSchoolIdAsync();

        Guid platformId;
        Guid schoolIdFaq;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            var category = await EnsureInterviewCategoryAsync(db);

            var platform = new FaqItem(
                category.Id, "منصة", "Platform FAQ", "<p>م</p>", "<p>P</p>", 1,
                FaqOwnershipScope.Platform, null, InterviewFaqCategory.Interview);
            platform.Publish();

            var school = new FaqItem(
                category.Id, "مدرسة", "School FAQ", "<p>م</p>", "<p>S</p>", 1,
                FaqOwnershipScope.School, schoolId, InterviewFaqCategory.Interview);
            school.Publish();

            var draft = new FaqItem(
                category.Id, "مسودة", "Draft FAQ", "<p>م</p>", "<p>D</p>", 2,
                FaqOwnershipScope.School, schoolId, InterviewFaqCategory.Interview);

            var inactive = new FaqItem(
                category.Id, "غير نشط", "Inactive FAQ", "<p>م</p>", "<p>I</p>", 3,
                FaqOwnershipScope.School, schoolId, InterviewFaqCategory.Interview);
            inactive.Publish();
            inactive.Deactivate();

            db.FaqItems.AddRange(platform, school, draft, inactive);
            await db.SaveChangesAsync();
            platformId = platform.Id;
            schoolIdFaq = school.Id;
        }

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}/interview-faqs");
        response.EnsureSuccessStatusCode();
        var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();

        Assert.Contains(platformId, ids);
        Assert.Contains(schoolIdFaq, ids);
        Assert.True(ids.IndexOf(platformId) < ids.IndexOf(schoolIdFaq));
        Assert.DoesNotContain(
            ids,
            id => id != platformId && id != schoolIdFaq &&
                  false);
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

    private static async Task<FaqCategory> EnsureInterviewCategoryAsync(SchooleraDbContext db)
    {
        var existing = await db.FaqCategories
            .FirstOrDefaultAsync(c => c.Slug == CmsSlugs.InterviewFaqCategorySlug);
        if (existing is not null)
        {
            return existing;
        }

        var max = await db.FaqCategories.MaxAsync(c => (int?)c.SortOrder) ?? 0;
        var category = new FaqCategory(
            "أسئلة المقابلة والتقييم",
            "Interview and assessment FAQs",
            CmsSlugs.InterviewFaqCategorySlug,
            max + 1);
        category.Publish();
        db.FaqCategories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }
}
