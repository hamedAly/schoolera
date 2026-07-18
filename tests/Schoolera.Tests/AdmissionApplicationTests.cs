using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Identity;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

/// <summary>
/// Shared helpers for parent admission-application integration tests.
/// Demo school public profile exposes grade IDs on offerings but not educationalStageId —
/// stage IDs are resolved via taxonomies by matching grade ownership.
/// </summary>
internal static class AdmissionTestHelpers
{
    public const string ApplicationsPath = "/api/parent/admission-applications";

    public static object SubmitConsentBody { get; } = new
    {
        termsAccepted = true,
        privacyAccepted = true,
    };

    /// <summary>
    /// Submit now returns <c>SubmitAdmissionOutcomeDto</c> (<c>application</c> + <c>missingRequirements</c>).
    /// Older tests that expect the application detail at <c>data</c> should use this helper.
    /// </summary>
    public static JsonElement UnwrapSubmitApplication(JsonElement resultData)
    {
        if (resultData.TryGetProperty("application", out var application) &&
            application.ValueKind == JsonValueKind.Object)
        {
            return application;
        }

        return resultData;
    }

    /// <summary>
    /// Minimal valid one-page PDF (signature, <c>/Type/Page</c>, and <c>%%EOF</c>).
    /// Empty-page stubs (<c>/Kids[]/Count 0</c>) are rejected by private storage.
    /// </summary>
    public static readonly byte[] MinimalPdfBytes = Encoding.ASCII.GetBytes(
        """
        %PDF-1.4
        1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
        2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
        3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]>>endobj
        trailer<</Root 1 0 R>>
        %%EOF
        """);

    /// <summary>
    /// Serializes mutating admission tests that share the seeded parent account
    /// (avoids duplicateActiveApplication collisions and login rate-limit bursts).
    /// </summary>
    public static readonly SemaphoreSlim MutatingGate = new(1, 1);

    private static readonly SemaphoreSlim InitGate = new(1, 1);
    private static readonly SemaphoreSlim ParentClientGate = new(1, 1);
    private static bool _demoSchoolReady;
    private static HttpClient? _sharedParentClient;
    private static WebApplicationFactory<Program>? _sharedParentFactory;

    public static async Task EnsureDemoSchoolAdmissionReadyAsync(WebApplicationFactory<Program> factory)
    {
        await InitGate.WaitAsync();
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();

                if (_demoSchoolReady)
                {
                    return;
                }

                // Keep published school-configurable requirements from polluting unrelated admission tests.
                await db.SchoolAdmissionRequirements
                    .Where(requirement => requirement.School.Slug == AuthTestHelpers.DemoSchoolSlug &&
                                          (requirement.IsActive ||
                                           requirement.PublicationStatus == AdmissionRequirementPublicationStatus.Published))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(requirement => requirement.IsActive, false)
                        .SetProperty(
                            requirement => requirement.PublicationStatus,
                            AdmissionRequirementPublicationStatus.Draft)
                        .SetProperty(requirement => requirement.UpdatedAtUtc, DateTimeOffset.UtcNow));

                await db.SchoolAdmissionQuestions
                    .Where(question => question.School.Slug == AuthTestHelpers.DemoSchoolSlug &&
                                       (question.IsActive ||
                                        question.PublicationStatus == AdmissionQuestionPublicationStatus.Published))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(question => question.IsActive, false)
                        .SetProperty(
                            question => question.PublicationStatus,
                            AdmissionQuestionPublicationStatus.Draft)
                        .SetProperty(question => question.UpdatedAtUtc, DateTimeOffset.UtcNow));

                await db.SchoolChildAgeEligibilityRules
                    .Where(rule => rule.School.Slug == AuthTestHelpers.DemoSchoolSlug &&
                                   (rule.IsActive ||
                                    rule.PublicationStatus == ChildAgeEligibilityPublicationStatus.Published))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(rule => rule.IsActive, false)
                        .SetProperty(
                            rule => rule.PublicationStatus,
                            ChildAgeEligibilityPublicationStatus.Draft)
                        .SetProperty(rule => rule.UpdatedAtUtc, DateTimeOffset.UtcNow));

                await db.Schools
                    .Where(item => item.Slug == AuthTestHelpers.DemoSchoolSlug)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Status, SchoolStatus.Published)
                        .SetProperty(item => item.GenderType, GenderType.Mixed)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));

                await db.SchoolBranches
                    .Where(branch => branch.School.Slug == AuthTestHelpers.DemoSchoolSlug)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(branch => branch.IsActive, true)
                        .SetProperty(branch => branch.UpdatedAtUtc, DateTimeOffset.UtcNow));

                await db.SchoolStageOfferings
                    .Where(offering => offering.SchoolBranch.School.Slug == AuthTestHelpers.DemoSchoolSlug)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(offering => offering.IsActive, true)
                        .SetProperty(offering => offering.IsAdmissionOpen, true)
                        .SetProperty(offering => offering.UpdatedAtUtc, DateTimeOffset.UtcNow));

                await db.SchoolGradeOfferings
                    .Where(grade => grade.SchoolStageOffering.SchoolBranch.School.Slug ==
                                    AuthTestHelpers.DemoSchoolSlug)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(grade => grade.IsActive, true));

                var school = await db.Schools
                    .AsNoTracking()
                    .Include(item => item.Branches)
                    .ThenInclude(branch => branch.StageOfferings)
                    .FirstAsync(item => item.Slug == AuthTestHelpers.DemoSchoolSlug);

                if (school.Branches.Count == 0)
                {
                    var city = await db.Cities.AsNoTracking().OrderBy(item => item.SortOrder).FirstAsync();
                    var district = await db.Districts.AsNoTracking()
                        .Where(item => item.CityId == city.Id)
                        .OrderBy(item => item.SortOrder)
                        .FirstAsync();
                    var branch = new SchoolBranch(
                        school.Id,
                        "فرع اختبار القبول",
                        "Admission Test Branch",
                        $"adm-test-{Guid.NewGuid():N}",
                        city.Id,
                        district.Id,
                        isMainBranch: true);
                    db.SchoolBranches.Add(branch);

                    var stages = await db.EducationalStages.AsNoTracking()
                        .Where(item => item.IsActive)
                        .OrderBy(item => item.SortOrder)
                        .Take(2)
                        .ToListAsync();
                    foreach (var stage in stages)
                    {
                        var gradeIds = await db.Grades.AsNoTracking()
                            .Where(item => item.EducationalStageId == stage.Id && item.IsActive)
                            .OrderBy(item => item.SortOrder)
                            .Select(item => item.Id)
                            .Take(2)
                            .ToListAsync();
                        if (gradeIds.Count == 0)
                        {
                            continue;
                        }

                        var offering = new SchoolStageOffering(
                            branch.Id,
                            stage.Id,
                            GenderType.Mixed,
                            capacity: 100,
                            isAdmissionOpen: true);
                        foreach (var gradeId in gradeIds)
                        {
                            offering.AddGradeOffering(new SchoolGradeOffering(offering.Id, gradeId));
                        }

                        db.SchoolStageOfferings.Add(offering);
                    }

                    await db.SaveChangesAsync();
                }
            }

            using var probe = factory.CreateClient();
            var context = await ResolveCreateContextFromPublicProfileAsync(probe);
            Assert.NotEmpty(context.Slots);
            Assert.Equal(
                AuthTestHelpers.DemoSchoolSlug,
                context.SchoolSlug,
                StringComparer.OrdinalIgnoreCase);

            _demoSchoolReady = true;
        }
        finally
        {
            InitGate.Release();
        }
    }

    /// <summary>
    /// Parent API cannot StartReview; force the domain transition when seed UnderReview rows are absent.
    /// </summary>
    public static async Task ForceUnderReviewAsync(
        WebApplicationFactory<Program> factory,
        Guid applicationId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var application = await db.AdmissionApplications
            .FirstAsync(item => item.Id == applicationId);
        Assert.Equal(AdmissionApplicationStatus.Submitted, application.Status);
        application.StartReview();
        db.AdmissionApplicationHistory.Add(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Submitted,
                toStatus: AdmissionApplicationStatus.UnderReview,
                action: AdmissionHistoryActions.ReviewStarted,
                actorUserId: application.ParentUserId,
                actorRole: "SchoolAdmin",
                parentVisible: true,
                parentVisibleNote: "test StartReview",
                internalNote: "integration fixture"));
        await db.SaveChangesAsync();
    }

    public static async Task ForceAcceptAsync(
        WebApplicationFactory<Program> factory,
        Guid applicationId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var application = await db.AdmissionApplications
            .FirstAsync(item => item.Id == applicationId);
        if (application.Status == AdmissionApplicationStatus.Submitted)
        {
            application.StartReview();
            db.AdmissionApplicationHistory.Add(
                new AdmissionApplicationHistory(
                    application.Id,
                    AdmissionApplicationStatus.Submitted,
                    AdmissionApplicationStatus.UnderReview,
                    AdmissionHistoryActions.ReviewStarted,
                    application.ParentUserId,
                    "SchoolAdmin",
                    parentVisible: true,
                    parentVisibleNote: "fixture review",
                    internalNote: "fixture"));
        }

        Assert.Equal(AdmissionApplicationStatus.UnderReview, application.Status);
        application.Accept();
        db.AdmissionApplicationHistory.Add(
            new AdmissionApplicationHistory(
                application.Id,
                AdmissionApplicationStatus.UnderReview,
                AdmissionApplicationStatus.Accepted,
                AdmissionHistoryActions.Accepted,
                application.ParentUserId,
                "SchoolAdmin",
                parentVisible: true,
                parentVisibleNote: "fixture accept",
                internalNote: "fixture"));
        await db.SaveChangesAsync();
    }

    public static async Task ForceRejectAsync(
        WebApplicationFactory<Program> factory,
        Guid applicationId,
        string rejectionReason = "fixture rejection",
        string schoolNotes = "internal school notes")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var application = await db.AdmissionApplications
            .FirstAsync(item => item.Id == applicationId);
        if (application.Status == AdmissionApplicationStatus.Submitted)
        {
            application.StartReview();
            db.AdmissionApplicationHistory.Add(
                new AdmissionApplicationHistory(
                    application.Id,
                    AdmissionApplicationStatus.Submitted,
                    AdmissionApplicationStatus.UnderReview,
                    AdmissionHistoryActions.ReviewStarted,
                    application.ParentUserId,
                    "SchoolAdmin",
                    parentVisible: true,
                    parentVisibleNote: "fixture review",
                    internalNote: "fixture"));
        }

        Assert.Equal(AdmissionApplicationStatus.UnderReview, application.Status);
        application.Reject(rejectionReason, schoolNotes);
        db.AdmissionApplicationHistory.Add(
            new AdmissionApplicationHistory(
                application.Id,
                AdmissionApplicationStatus.UnderReview,
                AdmissionApplicationStatus.Rejected,
                AdmissionHistoryActions.Rejected,
                application.ParentUserId,
                "SchoolAdmin",
                parentVisible: false,
                parentVisibleNote: null,
                internalNote: "internal rejection note"));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a confirmed Parent Identity user with a unique email and returns a logged-in cookie client.
    /// Caller owns disposal of the client.
    /// </summary>
    public static async Task<(HttpClient Client, string Email)> CreateSecondParentClientAsync(
        WebApplicationFactory<Program> factory)
    {
        var email = $"parent.adm.{Guid.NewGuid():N}@schoolera.local";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var now = DateTimeOffset.UtcNow;
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = "Second",
                LastName = "Parent",
                PreferredLanguage = "ar",
                AccountStatus = AccountStatus.Active,
                EmailConfirmed = true,
                PhoneNumber = $"+2010{Random.Shared.Next(10000000, 99999999)}",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            var create = await userManager.CreateAsync(user, AuthTestHelpers.DefaultPassword);
            Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(e => e.Description)));
            var role = await userManager.AddToRoleAsync(user, SchooleraRoles.Parent);
            Assert.True(role.Succeeded, string.Join("; ", role.Errors.Select(e => e.Description)));
        }

        var client = AuthTestHelpers.CreateCookieClient(factory);
        await LoginWithRetryAsync(client, email, AuthTestHelpers.DefaultPassword);
        return (client, email);
    }

    public static async Task RestoreDemoSchoolCatalogAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();

        await db.Schools
            .Where(item => item.Slug == AuthTestHelpers.DemoSchoolSlug)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, SchoolStatus.Published)
                .SetProperty(item => item.GenderType, GenderType.Mixed)
                .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));

        await db.SchoolBranches
            .Where(branch => branch.School.Slug == AuthTestHelpers.DemoSchoolSlug)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(branch => branch.IsActive, true)
                .SetProperty(branch => branch.UpdatedAtUtc, DateTimeOffset.UtcNow));

        await db.SchoolStageOfferings
            .Where(offering => offering.SchoolBranch.School.Slug == AuthTestHelpers.DemoSchoolSlug)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(offering => offering.IsActive, true)
                .SetProperty(offering => offering.IsAdmissionOpen, true)
                .SetProperty(offering => offering.UpdatedAtUtc, DateTimeOffset.UtcNow));

        await db.SchoolGradeOfferings
            .Where(grade => grade.SchoolStageOffering.SchoolBranch.School.Slug ==
                            AuthTestHelpers.DemoSchoolSlug)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(grade => grade.IsActive, true));
    }

    public static async Task<Guid> EnsureSecondActiveAcademicYearAsync(
        WebApplicationFactory<Program> factory,
        Guid excludeAcademicYearId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();

        var existingOther = await db.AcademicYears
            .Where(item => item.IsActive && item.Id != excludeAcademicYearId)
            .OrderByDescending(item => item.IsCurrent)
            .ThenBy(item => item.StartDate)
            .FirstOrDefaultAsync();
        if (existingOther is not null)
        {
            return existingOther.Id;
        }

        var reference = await db.AcademicYears
            .FirstAsync(item => item.Id == excludeAcademicYearId);

        var slug = $"adm-test-year-{Guid.NewGuid():N}"[..40];
        var year = new AcademicYear(
            "عام اختبار القبول",
            "Admission Test Year",
            slug,
            reference.StartDate.AddYears(1),
            reference.EndDate.AddYears(1),
            isCurrent: false);
        db.AcademicYears.Add(year);
        await db.SaveChangesAsync();
        return year.Id;
    }

    public static async Task<(Guid BranchId, Guid StageId, Guid UnofferedGradeId)> ResolveUnofferedGradeAsync(
        WebApplicationFactory<Program> factory,
        AdmissionCreateContext context)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var slot = context.Slots[0];

        var offeredGradeIds = await db.SchoolGradeOfferings
            .AsNoTracking()
            .Where(item =>
                item.SchoolStageOffering.SchoolBranchId == slot.BranchId &&
                item.SchoolStageOffering.EducationalStageId == slot.EducationalStageId &&
                item.SchoolStageOffering.IsActive &&
                item.IsActive)
            .Select(item => item.GradeId)
            .ToListAsync();

            var unoffered = await db.Grades
            .AsNoTracking()
            .Where(item =>
                item.EducationalStageId == slot.EducationalStageId &&
                item.IsActive &&
                !offeredGradeIds.Contains(item.Id))
            .OrderBy(item => item.SortOrder)
            .Select(item => item.Id)
            .FirstOrDefaultAsync();

        if (unoffered == Guid.Empty)
        {
            // Create an active grade under the same stage that is not offered.
            var grade = new Grade(
                slot.EducationalStageId,
                $"صف اختبار قبول {Random.Shared.Next(1000, 9999)}",
                $"Admission Test Grade {Random.Shared.Next(1000, 9999)}",
                $"adm-grade-{Guid.NewGuid():N}"[..40],
                sortOrder: 900 + Random.Shared.Next(1, 99));
            db.Grades.Add(grade);
            await db.SaveChangesAsync();
            unoffered = grade.Id;
        }

        return (slot.BranchId, slot.EducationalStageId, unoffered);
    }

    public static async Task<Guid> ResolveForeignBranchIdAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var foreign = await db.SchoolBranches
            .AsNoTracking()
            .Where(branch => branch.School.Slug != AuthTestHelpers.DemoSchoolSlug && branch.IsActive)
            .Select(branch => branch.Id)
            .FirstOrDefaultAsync();
        return foreign == Guid.Empty ? Guid.NewGuid() : foreign;
    }

    public static void AssertContainsErrorCode(JsonElement json, string expectedCode)
    {
        Assert.Contains(expectedCode, ReadErrorCodes(json));
    }

    /// <summary>
    /// Returns a shared cookie client for parent@schoolera.local. Do not dispose it.
    /// Call <see cref="RestoreParentCsrfAsync"/> after mutating DefaultRequestHeaders.
    /// </summary>
    public static async Task<HttpClient> GetSharedParentClientAsync(WebApplicationFactory<Program> factory)
    {
        await ParentClientGate.WaitAsync();
        try
        {
            if (_sharedParentClient is not null && ReferenceEquals(_sharedParentFactory, factory))
            {
                return _sharedParentClient;
            }

            _sharedParentClient?.Dispose();
            _sharedParentClient = AuthTestHelpers.CreateCookieClient(factory);
            _sharedParentFactory = factory;
            await LoginWithRetryAsync(
                _sharedParentClient,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);
            return _sharedParentClient;
        }
        finally
        {
            ParentClientGate.Release();
        }
    }

    public static async Task RestoreParentCsrfAsync(HttpClient client)
    {
        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);
    }

    public static async Task LoginWithRetryAsync(HttpClient client, string email, string password)
    {
        const int maxAttempts = 12;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await AuthTestHelpers.TryLoginAsync(client, email, password);
                return;
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("429", StringComparison.Ordinal) ||
                ex.Message.Contains("rateLimited", StringComparison.OrdinalIgnoreCase))
            {
                if (attempt == maxAttempts)
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, attempt * 3)));
            }
        }
    }

    public static async Task<AdmissionCreateContext> ResolveCreateContextAsync(
        WebApplicationFactory<Program> factory,
        HttpClient? client = null)
    {
        await EnsureDemoSchoolAdmissionReadyAsync(factory);
        await RestoreDemoSchoolCatalogAsync(factory);
        client ??= factory.CreateClient();
        return await ResolveCreateContextFromPublicProfileAsync(client);
    }

    public static async Task<AdmissionCreateContext> ResolveCreateContextFromPublicProfileAsync(HttpClient client)
    {
        var yearsResponse = await client.GetAsync("/api/taxonomies/academic-years");
        yearsResponse.EnsureSuccessStatusCode();
        var academicYearId = (await yearsResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

        var gradeToStage = await BuildGradeToStageMapAsync(client);

        // Prefer demo slug, then any published school with open offerings.
        var candidateSlugs = new List<string> { AuthTestHelpers.DemoSchoolSlug };
        var listResponse = await client.GetAsync("/api/schools?admissionOpen=true&pageNumber=1&pageSize=20");
        if (listResponse.IsSuccessStatusCode)
        {
            foreach (var item in (await listResponse.Content.ReadFromJsonAsync<JsonElement>())
                         .GetProperty("data").GetProperty("items").EnumerateArray())
            {
                var slug = item.GetProperty("slug").GetString();
                if (!string.IsNullOrWhiteSpace(slug) &&
                    !candidateSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase))
                {
                    candidateSlugs.Add(slug);
                }
            }
        }

        foreach (var slug in candidateSlugs)
        {
            var profileResponse = await client.GetAsync($"/api/schools/{slug}");
            if (!profileResponse.IsSuccessStatusCode)
            {
                continue;
            }

            var profile = (await profileResponse.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data");

            // Seeded test children are Male — skip girls-only schools.
            var genderType = profile.GetProperty("genderType").GetInt32();
            if (genderType is not ((int)GenderType.Boys or (int)GenderType.Mixed))
            {
                continue;
            }

            var slots = new List<AdmissionSlot>();
            foreach (var offering in profile.GetProperty("offerings").EnumerateArray())
            {
                if (!offering.GetProperty("isAdmissionOpen").GetBoolean())
                {
                    continue;
                }

                var branchId = offering.GetProperty("branchId").GetGuid();
                foreach (var grade in offering.GetProperty("grades").EnumerateArray())
                {
                    var gradeId = grade.GetProperty("id").GetGuid();
                    if (!gradeToStage.TryGetValue(gradeId, out var stageId))
                    {
                        continue;
                    }

                    slots.Add(new AdmissionSlot(branchId, stageId, gradeId));
                }
            }

            if (slots.Count == 0)
            {
                continue;
            }

            return new AdmissionCreateContext(
                profile.GetProperty("id").GetGuid(),
                profile.GetProperty("slug").GetString() ?? slug,
                academicYearId,
                slots);
        }

        Assert.Fail("No published school with open admission offerings was found for create tests.");
        return null!;
    }

    public static async Task<Guid> CreateChildAsync(
        HttpClient client,
        Guid? currentGradeId = null,
        int gender = (int)ChildGender.Male)
    {
        var gradeId = currentGradeId ?? await ResolveAnyActiveGradeIdAsync(client);
        // 14-digit NationalId-shaped value; ticks+random avoids parallel-suite collisions.
        var identity = $"299{DateTime.UtcNow.Ticks % 10_000_000_000L:D10}{Random.Shared.Next(0, 10)}";

        var create = await client.PostAsJsonAsync(
            "/api/parent/children",
            new
            {
                fullName = $"Admission Child {Random.Shared.Next(1000, 9999)}",
                identityType = 1,
                identityValue = identity,
                birthDate = "2015-05-01",
                gender,
                currentGradeId = gradeId,
                hasSpecialNeeds = false,
                specialNeedsNotes = (string?)null,
            });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        return (await create.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();
    }

    public static async Task<JsonElement> CreateDraftAsync(
        HttpClient client,
        AdmissionCreateContext context,
        Guid childProfileId,
        AdmissionSlot slot,
        string? parentNotes = null)
    {
        var response = await client.PostAsJsonAsync(
            ApplicationsPath,
            new
            {
                childProfileId,
                schoolId = (Guid?)null,
                schoolSlug = context.SchoolSlug,
                schoolBranchId = slot.BranchId,
                educationalStageId = slot.EducationalStageId,
                gradeId = slot.GradeId,
                academicYearId = context.AcademicYearId,
                parentNotes = parentNotes ?? "Integration draft",
            });

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Create draft failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        return json.GetProperty("data");
    }

    public static async Task<HttpResponseMessage> PostCreateAsync(
        HttpClient client,
        AdmissionCreateContext context,
        Guid childProfileId,
        AdmissionSlot slot)
    {
        return await client.PostAsJsonAsync(
            ApplicationsPath,
            new
            {
                childProfileId,
                schoolId = (Guid?)null,
                schoolSlug = context.SchoolSlug,
                schoolBranchId = slot.BranchId,
                educationalStageId = slot.EducationalStageId,
                gradeId = slot.GradeId,
                academicYearId = context.AcademicYearId,
                parentNotes = "CSRF probe",
            });
    }

    public static async Task<Guid> EnsureAtLeastOneApplicationAsync(
        WebApplicationFactory<Program> factory,
        HttpClient parentClient)
    {
        var list = await parentClient.GetAsync($"{ApplicationsPath}?pageNumber=1&pageSize=1");
        list.EnsureSuccessStatusCode();
        var items = (await list.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        if (items.Count > 0)
        {
            return items[0].GetProperty("id").GetGuid();
        }

        var context = await ResolveCreateContextAsync(factory, parentClient);
        var childId = await CreateChildAsync(parentClient, context.Slots[0].GradeId);
        var draft = await CreateDraftAsync(parentClient, context, childId, context.Slots[0]);
        return draft.GetProperty("id").GetGuid();
    }

    public static MultipartFormDataContent BuildPdfUploadContent(
        byte[] pdfBytes,
        AdmissionAttachmentType attachmentType = AdmissionAttachmentType.SupportingDocument)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(pdfBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "admission-fixture.pdf");
        content.Add(new StringContent(((int)attachmentType).ToString()), "attachmentType");
        return content;
    }

    public static IEnumerable<string?> ReadErrorCodes(JsonElement json) =>
        json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString());

    private static async Task<Dictionary<Guid, Guid>> BuildGradeToStageMapAsync(HttpClient client)
    {
        var stagesResponse = await client.GetAsync("/api/taxonomies/educational-stages");
        stagesResponse.EnsureSuccessStatusCode();
        var stages = (await stagesResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().ToList();

        var map = new Dictionary<Guid, Guid>();
        foreach (var stage in stages)
        {
            var stageId = stage.GetProperty("id").GetGuid();
            var gradesResponse = await client.GetAsync(
                $"/api/taxonomies/educational-stages/{stageId}/grades");
            gradesResponse.EnsureSuccessStatusCode();
            foreach (var grade in (await gradesResponse.Content.ReadFromJsonAsync<JsonElement>())
                         .GetProperty("data").EnumerateArray())
            {
                map[grade.GetProperty("id").GetGuid()] = stageId;
            }
        }

        return map;
    }

    private static async Task<Guid> ResolveAnyActiveGradeIdAsync(HttpClient client)
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

internal sealed record AdmissionSlot(Guid BranchId, Guid EducationalStageId, Guid GradeId);

internal sealed record AdmissionCreateContext(
    Guid SchoolId,
    string SchoolSlug,
    Guid AcademicYearId,
    IReadOnlyList<AdmissionSlot> Slots);

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationAuthorizationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationAuthorizationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(AdmissionTestHelpers.ApplicationsPath)]
    [InlineData(AdmissionTestHelpers.ApplicationsPath + "/00000000-0000-0000-0000-000000000001")]
    public async Task AdmissionGet_Anonymous_Returns401(string path)
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
    public async Task AdmissionList_NonParentRoles_Return403(string email)
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                client,
                email,
                AuthTestHelpers.DefaultPassword);
            var response = await client.GetAsync(AdmissionTestHelpers.ApplicationsPath);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task AdmissionList_Parent_Returns200()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var response = await client.GetAsync(AdmissionTestHelpers.ApplicationsPath);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("succeeded").GetBoolean());
            Assert.True(json.GetProperty("data").TryGetProperty("items", out _));
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task AdmissionCreate_SchoolOwner_Returns403()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory);
            var slot = context.Slots[0];

            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                client,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword);

            var response = await client.PostAsJsonAsync(
                AdmissionTestHelpers.ApplicationsPath,
                new
                {
                    childProfileId = Guid.NewGuid(),
                    schoolId = (Guid?)null,
                    schoolSlug = context.SchoolSlug,
                    schoolBranchId = slot.BranchId,
                    educationalStageId = slot.EducationalStageId,
                    gradeId = slot.GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "owner should be forbidden",
                });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationCsrfTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationCsrfTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /*
     * ASP.NET ValidateAntiForgeryToken requires BOTH the antiforgery cookie token
     * AND a matching X-XSRF-TOKEN header. AuthTestHelpers sets the header from the
     * XSRF-TOKEN cookie after login. Earlier PowerShell smoke tests may have appeared
     * to succeed without CSRF because they reused a session that still carried the
     * header from a prior /api/auth/me call, or inspected the wrong status — missing
     * or mismatched headers must yield 400 via AntiforgeryResultMiddleware, never 200.
     */

    [Fact]
    public async Task Create_WithValidCsrf_SucceedsOrReturnsNonAntiforgeryValidation()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            await AdmissionTestHelpers.RestoreParentCsrfAsync(client);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var slot = context.Slots[0];

            var response = await AdmissionTestHelpers.PostCreateAsync(client, context, childId, slot);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                var codes = AdmissionTestHelpers.ReadErrorCodes(json).ToList();
                Assert.False(
                    codes.Count == 1 && codes[0] == "error.validation",
                    "Valid CSRF header must not fail solely as antiforgery validation.");
            }
            else
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_MissingCsrfHeader_Returns400()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            await AdmissionTestHelpers.RestoreParentCsrfAsync(client);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);

            client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

            var response = await AdmissionTestHelpers.PostCreateAsync(
                client,
                context,
                childId,
                context.Slots[0]);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            await AdmissionTestHelpers.RestoreParentCsrfAsync(client);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_InvalidCsrfHeader_Returns400()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            await AdmissionTestHelpers.RestoreParentCsrfAsync(client);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);

            client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
            client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "invalid-token");

            var response = await AdmissionTestHelpers.PostCreateAsync(
                client,
                context,
                childId,
                context.Slots[0]);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            await AdmissionTestHelpers.RestoreParentCsrfAsync(client);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationIntegrationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationIntegrationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_RealCounts_ReflectCreateAndCancel()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var beforeResponse = await client.GetAsync("/api/parent/dashboard");
            Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);
            var before = (await beforeResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.True(before.GetProperty("applicationsAvailable").GetBoolean());
            var beforeTotal = before.GetProperty("totalApplications").GetInt32();
            var beforeDraft = before.GetProperty("draftApplications").GetInt32();
            var beforeCancelled = before.GetProperty("cancelledApplications").GetInt32();

            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var afterCreate = (await (await client.GetAsync("/api/parent/dashboard"))
                .Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("data");
            Assert.Equal(beforeTotal + 1, afterCreate.GetProperty("totalApplications").GetInt32());
            Assert.Equal(beforeDraft + 1, afterCreate.GetProperty("draftApplications").GetInt32());

            var cancel = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/cancel",
                new { reason = "dashboard count cancel" });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

            var afterCancel = (await (await client.GetAsync("/api/parent/dashboard"))
                .Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("data");
            Assert.Equal(beforeTotal + 1, afterCancel.GetProperty("totalApplications").GetInt32());
            Assert.Equal(beforeDraft, afterCancel.GetProperty("draftApplications").GetInt32());
            Assert.Equal(beforeCancelled + 1, afterCancel.GetProperty("cancelledApplications").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task List_Paging_Works()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            await AdmissionTestHelpers.EnsureAtLeastOneApplicationAsync(_factory, client);

            var page1 = await client.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}?pageNumber=1&pageSize=1");
            Assert.Equal(HttpStatusCode.OK, page1.StatusCode);
            var data = (await page1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal(1, data.GetProperty("pageNumber").GetInt32());
            Assert.Equal(1, data.GetProperty("pageSize").GetInt32());
            Assert.True(data.GetProperty("totalCount").GetInt32() >= 1);
            var items = data.GetProperty("items").EnumerateArray().ToList();
            Assert.True(items.Count <= 1);

            if (data.GetProperty("totalCount").GetInt32() > 1)
            {
                var page2 = await client.GetAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}?pageNumber=2&pageSize=1");
                Assert.Equal(HttpStatusCode.OK, page2.StatusCode);
                var page2Items = (await page2.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("data").GetProperty("items").EnumerateArray().ToList();
                Assert.NotEmpty(page2Items);
                if (items.Count == 1 && page2Items.Count == 1)
                {
                    Assert.NotEqual(
                        items[0].GetProperty("id").GetGuid(),
                        page2Items[0].GetProperty("id").GetGuid());
                }
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Detail_UnknownId_ReturnsSameNotFound()
    {
        // Cross-parent ownership: registering + verifying a second parent is non-trivial in
        // these fixtures (email verification). Random Guid covers the unknown/non-owned path
        // with the same notFound contract parents must not distinguish.
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var response = await client.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(AdmissionErrorCodes.NotFound, AdmissionTestHelpers.ReadErrorCodes(json));
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Detail_DoesNotExposeInternalFields()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var id = await AdmissionTestHelpers.EnsureAtLeastOneApplicationAsync(_factory, client);
            var detailResponse = await client.GetAsync($"{AdmissionTestHelpers.ApplicationsPath}/{id}");
            Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
            var data = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

            Assert.False(data.TryGetProperty("schoolNotes", out _));
            Assert.False(data.TryGetProperty("rejectionReason", out _));
            Assert.False(data.TryGetProperty("storageKey", out _));
            Assert.False(data.TryGetProperty("protectedIdentityValue", out _));

            if (data.TryGetProperty("attachments", out var attachments))
            {
                foreach (var attachment in attachments.EnumerateArray())
                {
                    Assert.False(attachment.TryGetProperty("storageKey", out _));
                    Assert.False(attachment.TryGetProperty("storedFileReference", out _));
                }
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_ValidDraft_Succeeds()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var data = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            Assert.Equal((int)AdmissionApplicationStatus.Draft, data.GetProperty("status").GetInt32());
            Assert.True(data.GetProperty("capabilities").GetProperty("canEdit").GetBoolean());
            Assert.True(data.GetProperty("capabilities").GetProperty("canSubmit").GetBoolean());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_DuplicateDraft_Blocked()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var slot = context.Slots[0];

            await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slot);
            var duplicate = await AdmissionTestHelpers.PostCreateAsync(client, context, childId, slot);
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
            var json = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(
                AdmissionErrorCodes.DuplicateActiveApplication,
                AdmissionTestHelpers.ReadErrorCodes(json));
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Submit_Draft_SetsSubmittedAndLocksEdit()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            if (submit.StatusCode != HttpStatusCode.OK)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Submit failed ({(int)submit.StatusCode}): {await submit.Content.ReadAsStringAsync()}");
            }
            var payload = (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            var data = AdmissionTestHelpers.UnwrapSubmitApplication(payload);
            Assert.Equal((int)AdmissionApplicationStatus.Submitted, data.GetProperty("status").GetInt32());
            Assert.NotEqual(JsonValueKind.Null, data.GetProperty("submittedAtUtc").ValueKind);
            Assert.False(data.GetProperty("capabilities").GetProperty("canEdit").GetBoolean());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Update_Submitted_BlockedAsReadOnly()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            submit.EnsureSuccessStatusCode();
            var submitted = AdmissionTestHelpers.UnwrapSubmitApplication(
                (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data"));
            var slot = context.Slots[0];

            var update = await client.PutAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}",
                new
                {
                    schoolBranchId = slot.BranchId,
                    educationalStageId = slot.EducationalStageId,
                    gradeId = slot.GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "should fail",
                    rowVersion = (byte[]?)null,
                });

            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
            var json = await update.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(AdmissionErrorCodes.ReadOnly, AdmissionTestHelpers.ReadErrorCodes(json));
            Assert.Equal(
                (int)AdmissionApplicationStatus.Submitted,
                submitted.GetProperty("status").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Cancel_Draft_Allowed()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var cancel = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/cancel",
                new { reason = "test cancel draft" });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            var data = (await cancel.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal((int)AdmissionApplicationStatus.Cancelled, data.GetProperty("status").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Cancel_Submitted_Allowed()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            submit.EnsureSuccessStatusCode();

            var cancel = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/cancel",
                new { reason = "test cancel submitted" });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            var data = (await cancel.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal((int)AdmissionApplicationStatus.Cancelled, data.GetProperty("status").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Cancel_UnderReview_Rejected()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            submit.EnsureSuccessStatusCode();

            await AdmissionTestHelpers.ForceUnderReviewAsync(_factory, applicationId);

            var cancel = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/cancel",
                new { reason = "should not cancel under review" });

            Assert.Equal(HttpStatusCode.BadRequest, cancel.StatusCode);
            var json = await cancel.Content.ReadFromJsonAsync<JsonElement>();
            var codes = AdmissionTestHelpers.ReadErrorCodes(json).ToList();
            Assert.True(
                codes.Contains(AdmissionErrorCodes.CancellationNotAllowed) ||
                codes.Contains(AdmissionErrorCodes.InvalidTransition),
                $"Expected cancellationNotAllowed or invalidTransition, got: {string.Join(',', codes)}");
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Attachment_UploadDownload_ThenBlockedAfterSubmit()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            using (var uploadContent = AdmissionTestHelpers.BuildPdfUploadContent(
                       AdmissionTestHelpers.MinimalPdfBytes))
            {
                var upload = await client.PostAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                    uploadContent);
                Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
                var uploaded = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
                var attachments = uploaded.GetProperty("attachments").EnumerateArray().ToList();
                Assert.NotEmpty(attachments);
                var attachmentId = attachments[0].GetProperty("id").GetGuid();
                Assert.False(attachments[0].TryGetProperty("storageKey", out _));

                var download = await client.GetAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments/{attachmentId}/download");
                Assert.Equal(HttpStatusCode.OK, download.StatusCode);
                Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
                var bytes = await download.Content.ReadAsByteArrayAsync();
                Assert.True(bytes.Length >= 5);
                Assert.Equal((byte)'%', bytes[0]);
                Assert.Equal((byte)'P', bytes[1]);
                Assert.Equal((byte)'D', bytes[2]);
                Assert.Equal((byte)'F', bytes[3]);
            }

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            using var blockedContent = AdmissionTestHelpers.BuildPdfUploadContent(
                AdmissionTestHelpers.MinimalPdfBytes);
            var blocked = await client.PostAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                blockedContent);
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
            var blockedJson = await blocked.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(
                AdmissionErrorCodes.AttachmentReadOnly,
                AdmissionTestHelpers.ReadErrorCodes(blockedJson));
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationOwnershipTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationOwnershipTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SecondParent_CannotAccessOwnedApplicationOrAttachment()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        HttpClient? secondClient = null;
        try
        {
            var owner = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, owner);
            var childId = await AdmissionTestHelpers.CreateChildAsync(owner, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                owner,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            Guid attachmentId;
            using (var uploadContent = AdmissionTestHelpers.BuildPdfUploadContent(
                       AdmissionTestHelpers.MinimalPdfBytes))
            {
                var upload = await owner.PostAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                    uploadContent);
                Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
                attachmentId = (await upload.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("data").GetProperty("attachments").EnumerateArray().First()
                    .GetProperty("id").GetGuid();
            }

            (secondClient, _) = await AdmissionTestHelpers.CreateSecondParentClientAsync(_factory);

            var list = await secondClient.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}?pageNumber=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var listIds = (await list.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid())
                .ToHashSet();
            Assert.DoesNotContain(applicationId, listIds);

            var get = await secondClient.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}");
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await get.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.NotFound);

            var slot = context.Slots[0];
            var put = await secondClient.PutAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}",
                new
                {
                    schoolBranchId = slot.BranchId,
                    educationalStageId = slot.EducationalStageId,
                    gradeId = slot.GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "should 404",
                    rowVersion = (byte[]?)null,
                });
            Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await put.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.NotFound);

            var submit = await secondClient.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.Equal(HttpStatusCode.NotFound, submit.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await submit.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.NotFound);

            var cancel = await secondClient.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/cancel",
                new { reason = "should 404" });
            Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await cancel.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.NotFound);

            var download = await secondClient.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments/{attachmentId}/download");
            Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
            var downloadJson = await download.Content.ReadFromJsonAsync<JsonElement>();
            var downloadCodes = AdmissionTestHelpers.ReadErrorCodes(downloadJson).ToList();
            Assert.True(
                downloadCodes.Contains(AdmissionErrorCodes.NotFound) ||
                downloadCodes.Contains(AdmissionErrorCodes.AttachmentNotFound),
                $"Expected notFound or attachmentNotFound, got: {string.Join(',', downloadCodes)}");
        }
        finally
        {
            secondClient?.Dispose();
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationValidationTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationValidationTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_InactiveChild_ReturnsStudentInactive()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                var child = await db.ChildProfiles.FirstAsync(item => item.Id == childId);
                child.Deactivate();
                await db.SaveChangesAsync();
            }

            var response = await AdmissionTestHelpers.PostCreateAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.StudentInactive);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Theory]
    [InlineData(SchoolStatus.Unpublished)]
    [InlineData(SchoolStatus.Suspended)]
    public async Task Create_UnpublishedOrSuspendedSchool_ReturnsSchoolNotAvailable(SchoolStatus status)
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.Schools
                    .Where(item => item.Id == context.SchoolId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Status, status)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));
            }

            var response = await client.PostAsJsonAsync(
                AdmissionTestHelpers.ApplicationsPath,
                new
                {
                    childProfileId = childId,
                    schoolId = context.SchoolId,
                    schoolSlug = (string?)null,
                    schoolBranchId = context.Slots[0].BranchId,
                    educationalStageId = context.Slots[0].EducationalStageId,
                    gradeId = context.Slots[0].GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "school unavailable",
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.SchoolNotAvailable);
        }
        finally
        {
            await AdmissionTestHelpers.RestoreDemoSchoolCatalogAsync(_factory);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_InactiveBranch_ReturnsBranchNotAvailable()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var slot = context.Slots[0];
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, slot.GradeId);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.SchoolBranches
                    .Where(item => item.Id == slot.BranchId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsActive, false)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));
            }

            var response = await AdmissionTestHelpers.PostCreateAsync(client, context, childId, slot);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.BranchNotAvailable);
        }
        finally
        {
            await AdmissionTestHelpers.RestoreDemoSchoolCatalogAsync(_factory);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_BranchNotBelongingToSchool_ReturnsBranchNotAvailable()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var foreignBranchId = await AdmissionTestHelpers.ResolveForeignBranchIdAsync(_factory);
            var slot = context.Slots[0];

            var response = await client.PostAsJsonAsync(
                AdmissionTestHelpers.ApplicationsPath,
                new
                {
                    childProfileId = childId,
                    schoolId = (Guid?)null,
                    schoolSlug = context.SchoolSlug,
                    schoolBranchId = foreignBranchId,
                    educationalStageId = slot.EducationalStageId,
                    gradeId = slot.GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "foreign branch",
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.BranchNotAvailable);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_InvalidStageGradeRelation_ReturnsInvalidStageGrade()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var slot = context.Slots[0];

            Guid wrongStageId;
            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                wrongStageId = await db.EducationalStages
                    .AsNoTracking()
                    .Where(item => item.Id != slot.EducationalStageId && item.IsActive)
                    .Select(item => item.Id)
                    .FirstAsync();
            }

            var response = await client.PostAsJsonAsync(
                AdmissionTestHelpers.ApplicationsPath,
                new
                {
                    childProfileId = childId,
                    schoolId = (Guid?)null,
                    schoolSlug = context.SchoolSlug,
                    schoolBranchId = slot.BranchId,
                    educationalStageId = wrongStageId,
                    gradeId = slot.GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "mismatched stage/grade",
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.InvalidStageGrade);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_GradeNotOffered_ReturnsInvalidStageGrade()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var (_, stageId, unofferedGradeId) =
                await AdmissionTestHelpers.ResolveUnofferedGradeAsync(_factory, context);

            var response = await client.PostAsJsonAsync(
                AdmissionTestHelpers.ApplicationsPath,
                new
                {
                    childProfileId = childId,
                    schoolId = (Guid?)null,
                    schoolSlug = context.SchoolSlug,
                    schoolBranchId = context.Slots[0].BranchId,
                    educationalStageId = stageId,
                    gradeId = unofferedGradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "grade not offered",
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.InvalidStageGrade);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_AdmissionClosed_ReturnsAdmissionClosed()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var slot = context.Slots[0];
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, slot.GradeId);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.SchoolStageOfferings
                    .Where(item =>
                        item.SchoolBranchId == slot.BranchId &&
                        item.EducationalStageId == slot.EducationalStageId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsAdmissionOpen, false)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));
            }

            var response = await AdmissionTestHelpers.PostCreateAsync(client, context, childId, slot);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.AdmissionClosed);
        }
        finally
        {
            await AdmissionTestHelpers.RestoreDemoSchoolCatalogAsync(_factory);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_InactiveAcademicYear_ReturnsInvalidAcademicYear()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        var yearId = Guid.Empty;
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            yearId = context.AcademicYearId;
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.AcademicYears
                    .Where(item => item.Id == yearId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsActive, false)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));
            }

            var response = await AdmissionTestHelpers.PostCreateAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.InvalidAcademicYear);
        }
        finally
        {
            if (yearId != Guid.Empty)
            {
                await using var scope = _factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.AcademicYears
                    .Where(item => item.Id == yearId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsActive, true)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));
            }

            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_GenderMismatch_ReturnsGenderNotEligible()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(
                client,
                context.Slots[0].GradeId,
                gender: (int)ChildGender.Male);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.Schools
                    .Where(item => item.Id == context.SchoolId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.GenderType, GenderType.Girls)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));
            }

            var response = await AdmissionTestHelpers.PostCreateAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await response.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.GenderNotEligible);
        }
        finally
        {
            await AdmissionTestHelpers.RestoreDemoSchoolCatalogAsync(_factory);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationDuplicateTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationDuplicateTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_DuplicateSubmitted_Blocked()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var slot = context.Slots[0];
            var draft = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slot);
            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{draft.GetProperty("id").GetGuid()}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            var duplicate = await AdmissionTestHelpers.PostCreateAsync(client, context, childId, slot);
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await duplicate.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.DuplicateActiveApplication);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_DuplicateUnderReviewAndAccepted_Blocked()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var underReviewChild = await AdmissionTestHelpers.CreateChildAsync(
                client,
                context.Slots[0].GradeId);
            var acceptedChild = await AdmissionTestHelpers.CreateChildAsync(
                client,
                context.Slots[0].GradeId);
            var slot = context.Slots[0];

            var underReviewDraft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                underReviewChild,
                slot);
            var underReviewId = underReviewDraft.GetProperty("id").GetGuid();
            (await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{underReviewId}/submit",
                new { termsAccepted = true, privacyAccepted = true })).EnsureSuccessStatusCode();
            await AdmissionTestHelpers.ForceUnderReviewAsync(_factory, underReviewId);

            var underReviewDup = await AdmissionTestHelpers.PostCreateAsync(
                client,
                context,
                underReviewChild,
                slot);
            Assert.Equal(HttpStatusCode.BadRequest, underReviewDup.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await underReviewDup.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.DuplicateActiveApplication);

            var acceptedDraft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                acceptedChild,
                slot);
            var acceptedId = acceptedDraft.GetProperty("id").GetGuid();
            (await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{acceptedId}/submit",
                new { termsAccepted = true, privacyAccepted = true })).EnsureSuccessStatusCode();
            await AdmissionTestHelpers.ForceAcceptAsync(_factory, acceptedId);

            var acceptedDup = await AdmissionTestHelpers.PostCreateAsync(
                client,
                context,
                acceptedChild,
                slot);
            Assert.Equal(HttpStatusCode.BadRequest, acceptedDup.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await acceptedDup.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.DuplicateActiveApplication);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_ConcurrentDuplicate_OneSucceedsOneFails()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory);
            var slot = context.Slots[0];

            using var clientA = AuthTestHelpers.CreateCookieClient(_factory);
            using var clientB = AuthTestHelpers.CreateCookieClient(_factory);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                clientA,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                clientB,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);

            var childId = await AdmissionTestHelpers.CreateChildAsync(clientA, slot.GradeId);

            var taskA = AdmissionTestHelpers.PostCreateAsync(clientA, context, childId, slot);
            var taskB = AdmissionTestHelpers.PostCreateAsync(clientB, context, childId, slot);
            var responses = await Task.WhenAll(taskA, taskB);

            var okCount = responses.Count(item => item.StatusCode == HttpStatusCode.OK);
            var badCount = responses.Count(item => item.StatusCode == HttpStatusCode.BadRequest);
            Assert.Equal(1, okCount);
            Assert.Equal(1, badCount);

            var bad = responses.First(item => item.StatusCode == HttpStatusCode.BadRequest);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await bad.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.DuplicateActiveApplication);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_AfterCancelled_PermitsNew()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var slot = context.Slots[0];
            var draft = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slot);
            var cancel = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{draft.GetProperty("id").GetGuid()}/cancel",
                new { reason = "allow replacement" });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

            var again = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slot);
            Assert.Equal((int)AdmissionApplicationStatus.Draft, again.GetProperty("status").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_AfterRejected_PermitsNew()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var slot = context.Slots[0];
            var draft = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slot);
            var applicationId = draft.GetProperty("id").GetGuid();
            (await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true })).EnsureSuccessStatusCode();
            await AdmissionTestHelpers.ForceRejectAsync(_factory, applicationId);

            var again = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slot);
            Assert.Equal((int)AdmissionApplicationStatus.Draft, again.GetProperty("status").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Create_DifferentAcademicYearOrBranchGrade_Allowed()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            Assert.True(context.Slots.Count >= 1);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var primarySlot = context.Slots[0];
            await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, primarySlot);

            var secondYearId = await AdmissionTestHelpers.EnsureSecondActiveAcademicYearAsync(
                _factory,
                context.AcademicYearId);
            Assert.NotEqual(context.AcademicYearId, secondYearId);
            var yearVariant = await client.PostAsJsonAsync(
                AdmissionTestHelpers.ApplicationsPath,
                new
                {
                    childProfileId = childId,
                    schoolId = (Guid?)null,
                    schoolSlug = context.SchoolSlug,
                    schoolBranchId = primarySlot.BranchId,
                    educationalStageId = primarySlot.EducationalStageId,
                    gradeId = primarySlot.GradeId,
                    academicYearId = secondYearId,
                    parentNotes = "different year",
                });
            Assert.True(
                yearVariant.StatusCode == HttpStatusCode.OK,
                $"Different-year create failed ({(int)yearVariant.StatusCode}): {await yearVariant.Content.ReadAsStringAsync()}");

            var alternateSlot = context.Slots.FirstOrDefault(item =>
                item.GradeId != primarySlot.GradeId || item.BranchId != primarySlot.BranchId);
            if (alternateSlot is null)
            {
                return;
            }

            var gradeVariant = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                alternateSlot);
            Assert.Equal((int)AdmissionApplicationStatus.Draft, gradeVariant.GetProperty("status").GetInt32());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationDraftUpdateTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationDraftUpdateTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_Draft_Succeeds()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();
            var slot = context.Slots[0];

            var update = await client.PutAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}",
                new
                {
                    schoolBranchId = slot.BranchId,
                    educationalStageId = slot.EducationalStageId,
                    gradeId = slot.GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "updated draft notes",
                    rowVersion = (byte[]?)null,
                });

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var data = (await update.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal("updated draft notes", data.GetProperty("parentNotes").GetString());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Update_InvalidGrade_Revalidates()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();
            var (_, stageId, unofferedGradeId) =
                await AdmissionTestHelpers.ResolveUnofferedGradeAsync(_factory, context);

            var update = await client.PutAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}",
                new
                {
                    schoolBranchId = context.Slots[0].BranchId,
                    educationalStageId = stageId,
                    gradeId = unofferedGradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "invalid grade update",
                    rowVersion = (byte[]?)null,
                });

            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await update.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.InvalidStageGrade);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Update_IntroducingDuplicate_Blocked()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            Assert.True(
                context.Slots.Count >= 2,
                "Need at least two admission slots to test update-into-duplicate.");
            var slotA = context.Slots[0];
            var slotB = context.Slots.First(item =>
                item.GradeId != slotA.GradeId || item.BranchId != slotA.BranchId);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, slotA.GradeId);

            await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slotA);
            var movable = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slotB);
            var movableId = movable.GetProperty("id").GetGuid();

            var update = await client.PutAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{movableId}",
                new
                {
                    schoolBranchId = slotA.BranchId,
                    educationalStageId = slotA.EducationalStageId,
                    gradeId = slotA.GradeId,
                    academicYearId = context.AcademicYearId,
                    parentNotes = "collide",
                    rowVersion = (byte[]?)null,
                });

            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await update.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.DuplicateActiveApplication);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Update_ExtraParentOrChildIds_Ignored()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var otherChildId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();
            var slot = context.Slots[0];

            var payload = new Dictionary<string, object?>
            {
                ["schoolBranchId"] = slot.BranchId,
                ["educationalStageId"] = slot.EducationalStageId,
                ["gradeId"] = slot.GradeId,
                ["academicYearId"] = context.AcademicYearId,
                ["parentNotes"] = "extra ids ignored",
                ["rowVersion"] = null,
                ["parentUserId"] = Guid.NewGuid(),
                ["childProfileId"] = otherChildId,
            };

            var update = await client.PutAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}",
                payload);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var data = (await update.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.Equal(childId, data.GetProperty("childProfileId").GetGuid());
            Assert.Equal("extra ids ignored", data.GetProperty("parentNotes").GetString());
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationSubmissionExtraTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationSubmissionExtraTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Submit_CapturesSnapshotsAndHistory()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
            var detail = AdmissionTestHelpers.UnwrapSubmitApplication(
                (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data"));
            Assert.Contains(
                detail.GetProperty("timeline").EnumerateArray(),
                item => item.GetProperty("action").GetString() == AdmissionHistoryActions.Submitted);

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            var entity = await db.AdmissionApplications.AsNoTracking()
                .FirstAsync(item => item.Id == applicationId);
            Assert.False(string.IsNullOrWhiteSpace(entity.SubmittedChildFullName));
            Assert.False(string.IsNullOrWhiteSpace(entity.SubmittedSchoolNameAr));
            Assert.False(string.IsNullOrWhiteSpace(entity.SubmittedBranchNameAr));
            Assert.False(string.IsNullOrWhiteSpace(entity.SubmittedStageNameAr));
            Assert.False(string.IsNullOrWhiteSpace(entity.SubmittedGradeNameAr));
            Assert.False(string.IsNullOrWhiteSpace(entity.SubmittedAcademicYearNameAr));
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Submit_AfterAdmissionCloses_ReturnsAdmissionClosed()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var slot = context.Slots[0];
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, slot.GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(client, context, childId, slot);
            var applicationId = draft.GetProperty("id").GetGuid();

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                await db.SchoolStageOfferings
                    .Where(item =>
                        item.SchoolBranchId == slot.BranchId &&
                        item.EducationalStageId == slot.EducationalStageId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsAdmissionOpen, false)
                        .SetProperty(item => item.UpdatedAtUtc, DateTimeOffset.UtcNow));
            }

            var submit = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await submit.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.AdmissionClosed);
        }
        finally
        {
            await AdmissionTestHelpers.RestoreDemoSchoolCatalogAsync(_factory);
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Submit_DoubleSubmit_InvalidTransition()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            (await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true })).EnsureSuccessStatusCode();

            var second = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true });
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            var codes = AdmissionTestHelpers.ReadErrorCodes(
                await second.Content.ReadFromJsonAsync<JsonElement>()).ToList();
            Assert.True(
                codes.Contains(AdmissionErrorCodes.InvalidTransition) ||
                codes.Contains(AdmissionErrorCodes.ReadOnly),
                $"Expected invalidTransition or readOnly, got: {string.Join(',', codes)}");
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationCancellationExtraTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationCancellationExtraTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Cancel_Accepted_Rejected()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();
            (await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/submit",
                new { termsAccepted = true, privacyAccepted = true })).EnsureSuccessStatusCode();
            await AdmissionTestHelpers.ForceAcceptAsync(_factory, applicationId);

            var cancel = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/cancel",
                new { reason = "accepted cannot cancel" });
            Assert.Equal(HttpStatusCode.BadRequest, cancel.StatusCode);
            var codes = AdmissionTestHelpers.ReadErrorCodes(
                await cancel.Content.ReadFromJsonAsync<JsonElement>()).ToList();
            Assert.True(
                codes.Contains(AdmissionErrorCodes.CancellationNotAllowed) ||
                codes.Contains(AdmissionErrorCodes.InvalidTransition),
                $"Expected cancellationNotAllowed or invalidTransition, got: {string.Join(',', codes)}");
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Cancel_CreatesHistory()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            var cancel = await client.PostAsJsonAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/cancel",
                new { reason = "history cancel" });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            var timeline = (await cancel.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").GetProperty("timeline").EnumerateArray().ToList();
            Assert.Contains(
                timeline,
                item => item.GetProperty("action").GetString() == AdmissionHistoryActions.Cancelled);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationAttachmentExtraTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationAttachmentExtraTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Attachment_UnsupportedType_ReturnsAttachmentTypeInvalid()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            using var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes("not a document"));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(file, "file", "notes.txt");
            content.Add(new StringContent(((int)AdmissionAttachmentType.SupportingDocument).ToString()), "attachmentType");

            var upload = await client.PostAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                content);
            Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await upload.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.AttachmentTypeInvalid);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Attachment_Oversized_ReturnsAttachmentTooLarge()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["PrivateFileStorage:MaxFileSizeBytes"] = "512",
                    });
                });
            });

            using var client = AuthTestHelpers.CreateCookieClient(factory);
            await AdmissionTestHelpers.LoginWithRetryAsync(
                client,
                AuthTestHelpers.ParentEmail,
                AuthTestHelpers.DefaultPassword);

            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            long maxBytes;
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                maxBytes = scope.ServiceProvider
                    .GetRequiredService<IPrivateFileStorage>()
                    .Policy.MaxFileSizeBytes;
            }

            var oversized = new byte[maxBytes + 64];
            Encoding.ASCII.GetBytes("%PDF-1.4").CopyTo(oversized, 0);

            using var uploadContent = AdmissionTestHelpers.BuildPdfUploadContent(oversized);
            var upload = await client.PostAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                uploadContent);
            Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
            AdmissionTestHelpers.AssertContainsErrorCode(
                await upload.Content.ReadFromJsonAsync<JsonElement>(),
                AdmissionErrorCodes.AttachmentTooLarge);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Attachment_DraftDelete_RemovesMetadataAndPhysicalFile()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            Guid attachmentId;
            string storageKey;
            using (var uploadContent = AdmissionTestHelpers.BuildPdfUploadContent(
                       AdmissionTestHelpers.MinimalPdfBytes))
            {
                var upload = await client.PostAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments",
                    uploadContent);
                Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
                attachmentId = (await upload.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("data").GetProperty("attachments").EnumerateArray().First()
                    .GetProperty("id").GetGuid();
            }

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                storageKey = await db.Set<AdmissionApplicationAttachment>()
                    .AsNoTracking()
                    .Where(item => item.Id == attachmentId)
                    .Select(item => item.StorageKey)
                    .FirstAsync();
                Assert.False(string.IsNullOrWhiteSpace(storageKey));
            }

            var delete = await client.DeleteAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}/attachments/{attachmentId}");
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
            var remaining = (await delete.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").GetProperty("attachments").EnumerateArray().ToList();
            Assert.DoesNotContain(remaining, item => item.GetProperty("id").GetGuid() == attachmentId);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                Assert.False(await db.Set<AdmissionApplicationAttachment>()
                    .AnyAsync(item => item.Id == attachmentId));

                var storage = scope.ServiceProvider.GetRequiredService<IPrivateFileStorage>();
                await using var stream = await storage.OpenReadAsync(storageKey);
                Assert.Null(stream);
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationHistoryDtoTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationHistoryDtoTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Detail_ExcludesInternalHistoryAndSensitiveFields()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var draft = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childId,
                context.Slots[0]);
            var applicationId = draft.GetProperty("id").GetGuid();

            Guid hiddenHistoryId;
            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                var application = await db.AdmissionApplications
                    .FirstAsync(item => item.Id == applicationId);
                await db.AdmissionApplications
                    .Where(item => item.Id == applicationId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.SchoolNotes, "secret-school-notes")
                        .SetProperty(item => item.RejectionReason, "secret-rejection"));

                var hidden = new AdmissionApplicationHistory(
                    applicationId,
                    application.Status,
                    application.Status,
                    "InternalReviewNote",
                    application.ParentUserId,
                    "SchoolAdmin",
                    parentVisible: false,
                    parentVisibleNote: "should not appear",
                    internalNote: "internal only");
                db.AdmissionApplicationHistory.Add(hidden);
                await db.SaveChangesAsync();
                hiddenHistoryId = hidden.Id;
            }

            var detailResponse = await client.GetAsync(
                $"{AdmissionTestHelpers.ApplicationsPath}/{applicationId}");
            Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
            var raw = await detailResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("secret-school-notes", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("secret-rejection", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("internal only", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("should not appear", raw, StringComparison.Ordinal);

            var data = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.False(data.TryGetProperty("schoolNotes", out _));
            Assert.False(data.TryGetProperty("rejectionReason", out _));
            Assert.False(data.TryGetProperty("storageKey", out _));
            Assert.False(data.TryGetProperty("protectedIdentityValue", out _));
            Assert.False(data.TryGetProperty("internalNote", out _));

            var timelineIds = data.GetProperty("timeline").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid())
                .ToHashSet();
            Assert.DoesNotContain(hiddenHistoryId, timelineIds);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}

public sealed class AdmissionApplicationCapabilitiesUnitTests
{
    [Theory]
    [InlineData(AdmissionApplicationStatus.Draft, true, true, true, true, true)]
    [InlineData(AdmissionApplicationStatus.Submitted, false, false, true, false, false)]
    [InlineData(AdmissionApplicationStatus.UnderReview, false, false, false, false, false)]
    [InlineData(AdmissionApplicationStatus.MissingDocuments, false, false, false, true, true)]
    [InlineData(AdmissionApplicationStatus.Accepted, false, false, false, false, false)]
    [InlineData(AdmissionApplicationStatus.Rejected, false, false, false, false, false)]
    [InlineData(AdmissionApplicationStatus.Cancelled, false, false, false, false, false)]
    [InlineData(AdmissionApplicationStatus.Registered, false, false, false, false, false)]
    public void Capabilities_MatchTransitionPolicy(
        AdmissionApplicationStatus status,
        bool canEdit,
        bool canSubmit,
        bool canCancel,
        bool canUpload,
        bool canRemove)
    {
        DateTimeOffset? reviewStarted = status == AdmissionApplicationStatus.Submitted
            ? null
            : status is AdmissionApplicationStatus.UnderReview
                or AdmissionApplicationStatus.Accepted
                or AdmissionApplicationStatus.Rejected
                ? DateTimeOffset.UtcNow
                : null;

        var caps = AdmissionCapabilityFactory.From(status, reviewStarted);
        Assert.Equal(canEdit, caps.CanEdit);
        Assert.Equal(canSubmit, caps.CanSubmit);
        Assert.Equal(canCancel, caps.CanCancel);
        Assert.Equal(canUpload, caps.CanUploadAttachments);
        Assert.Equal(canRemove, caps.CanRemoveAttachments);
    }
}

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionApplicationNumberTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private static readonly Regex ApplicationNumberPattern = new(
        @"^APP-\d{4}-\d{6}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionApplicationNumberTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ApplicationNumbers_UniqueAndMatchFormat()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            var client = await AdmissionTestHelpers.GetSharedParentClientAsync(_factory);
            var context = await AdmissionTestHelpers.ResolveCreateContextAsync(_factory, client);
            var childA = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
            var childB = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);

            var draftA = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childA,
                context.Slots[0]);
            var draftB = await AdmissionTestHelpers.CreateDraftAsync(
                client,
                context,
                childB,
                context.Slots[0]);

            var numberA = draftA.GetProperty("applicationNumber").GetString();
            var numberB = draftB.GetProperty("applicationNumber").GetString();
            Assert.False(string.IsNullOrWhiteSpace(numberA));
            Assert.False(string.IsNullOrWhiteSpace(numberB));
            Assert.Matches(ApplicationNumberPattern, numberA!);
            Assert.Matches(ApplicationNumberPattern, numberB!);
            Assert.NotEqual(numberA, numberB);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }
}
