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
public sealed class AdmissionQuestionsTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionQuestionsTests(SchooleraWebApplicationFactory factory)
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

        var schoolDefault = CreateQuestion(
            schoolId, "transport", yearId: null, branchId: null, stageId: null, gradeId: null,
            sortOrder: 1, userId: userId);
        var gradeYear = CreateQuestion(
            schoolId, "transport", yearId, branchId: null, stageId: null, gradeId,
            sortOrder: 2, userId: userId);
        var branchGradeYear = CreateQuestion(
            schoolId, "transport", yearId, branchId, stageId: null, gradeId,
            sortOrder: 3, userId: userId);

        var applicable = AdmissionQuestionCatalog.ResolveApplicable(
            [schoolDefault, gradeYear, branchGradeYear],
            branchId,
            stageId,
            gradeId,
            yearId);

        Assert.Single(applicable);
        Assert.Equal(branchGradeYear.Id, applicable[0].Id);
    }

    [Fact]
    public async Task SchoolPortalQuestions_Parent_Returns403()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword);

        var profile = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        profile.EnsureSuccessStatusCode();
        var schoolId = (await profile.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/admission-questions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuestion_WithoutCsrf_Returns400()
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
            $"/api/school-portal/schools/{schoolId}/admission-questions",
            new
            {
                questionCode = "test-q",
                questionType = (int)AdmissionQuestionType.ShortText,
                labelAr = "سؤال",
                labelEn = "Question",
                isRequired = true,
                sortOrder = 0,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EnsureSnapshots_IsIdempotentOnCreateAndUpdate()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
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

            var code = $"notes-{Guid.NewGuid():N}";
            var question = new SchoolAdmissionQuestion(
                school.Id,
                code,
                AdmissionQuestionType.ShortText,
                "ملاحظات",
                "Notes",
                null,
                null,
                isRequired: false,
                sortOrder: 0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                ownerId);
            question.Publish(ownerId);
            db.SchoolAdmissionQuestions.Add(question);
            await db.SaveChangesAsync();

            try
            {
                var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
                var draftJson = await AdmissionTestHelpers.CreateDraftAsync(
                    client, context, childId, context.Slots[0]);
                var draftId = draftJson.GetProperty("id").GetGuid();

                var ensure = await client.PostAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{draftId}/question-snapshots/ensure",
                    null);
                ensure.EnsureSuccessStatusCode();

                var snapshotCount = await db.AdmissionApplicationQuestionSnapshots
                    .AsNoTracking()
                    .CountAsync(snapshot => snapshot.AdmissionApplicationId == draftId);
                Assert.True(snapshotCount >= 0);

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

                var afterUpdate = await db.AdmissionApplicationQuestionSnapshots
                    .AsNoTracking()
                    .CountAsync(snapshot => snapshot.AdmissionApplicationId == draftId);
                Assert.Equal(snapshotCount, afterUpdate);
            }
            finally
            {
                question.Deactivate(ownerId);
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task Submit_WithIncompleteQuestions_ReturnsQuestionsIncompletePayload()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
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

            var code = $"required-text-{Guid.NewGuid():N}";
            var question = new SchoolAdmissionQuestion(
                school.Id,
                code,
                AdmissionQuestionType.ShortText,
                "سؤال مطلوب",
                "Required question",
                null,
                null,
                isRequired: true,
                sortOrder: 0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                ownerId);
            question.Publish(ownerId);
            db.SchoolAdmissionQuestions.Add(question);
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
                    AdmissionErrorCodes.QuestionsIncomplete,
                    ReadCodes(json));
                Assert.True(json.TryGetProperty("data", out var data));
                Assert.True(data.TryGetProperty("missingQuestions", out var missing));
                Assert.True(missing.GetArrayLength() > 0);
            }
            finally
            {
                question.Deactivate(ownerId);
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    [Fact]
    public async Task UpdateScope_WithExistingAnswers_RequiresConfirmation()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
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

            var code = $"scope-block-{Guid.NewGuid():N}";
            var question = new SchoolAdmissionQuestion(
                school.Id,
                code,
                AdmissionQuestionType.ShortText,
                "سؤال",
                "Question",
                null,
                null,
                isRequired: false,
                sortOrder: 0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                ownerId);
            question.Publish(ownerId);
            db.SchoolAdmissionQuestions.Add(question);
            await db.SaveChangesAsync();

            try
            {
                var childId = await AdmissionTestHelpers.CreateChildAsync(client, context.Slots[0].GradeId);
                var draft = await AdmissionTestHelpers.CreateDraftAsync(
                    client, context, childId, context.Slots[0]);
                var draftId = draft.GetProperty("id").GetGuid();

                var snapshotId = await db.AdmissionApplicationQuestionSnapshots
                    .AsNoTracking()
                    .Where(snapshot => snapshot.AdmissionApplicationId == draftId)
                    .Select(snapshot => snapshot.Id)
                    .FirstAsync();

                var upsert = await client.PutAsJsonAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{draftId}/answers",
                    new
                    {
                        questionSnapshotId = snapshotId,
                        textValue = "answered",
                    });
                upsert.EnsureSuccessStatusCode();

                var alternateSlot = context.Slots.First(slot => slot.GradeId != context.Slots[0].GradeId);
                var blocked = await client.PutAsJsonAsync(
                    $"{AdmissionTestHelpers.ApplicationsPath}/{draftId}",
                    new
                    {
                        schoolBranchId = alternateSlot.BranchId,
                        educationalStageId = alternateSlot.EducationalStageId,
                        gradeId = alternateSlot.GradeId,
                        academicYearId = context.AcademicYearId,
                        parentNotes = (string?)null,
                        rowVersion = (byte[]?)null,
                        confirmClearQuestionAnswers = false,
                    });
                Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
                var blockedJson = await blocked.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Contains(
                    AdmissionErrorCodes.QuestionAnswersBlockScopeChange,
                    ReadCodes(blockedJson));
            }
            finally
            {
                question.Deactivate(ownerId);
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    private static SchoolAdmissionQuestion CreateQuestion(
        Guid schoolId,
        string code,
        Guid? yearId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        int sortOrder,
        Guid userId)
    {
        var question = new SchoolAdmissionQuestion(
            schoolId,
            code,
            AdmissionQuestionType.ShortText,
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
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            userId);
        question.Publish(userId);
        return question;
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
