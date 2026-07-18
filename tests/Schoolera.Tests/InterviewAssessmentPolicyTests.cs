using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Integrations;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class InterviewAssessmentPolicyTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public InterviewAssessmentPolicyTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ResolveApplicable_PicksMostSpecificPublishedPolicy()
    {
        var schoolId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var schoolDefault = CreatePolicy(schoolId, userId, yearId: null, branchId: null, stageId: null, gradeId: null);
        var gradeYear = CreatePolicy(schoolId, userId, yearId, branchId: null, stageId: null, gradeId);
        var branchGradeYear = CreatePolicy(schoolId, userId, yearId, branchId, stageId: null, gradeId);

        schoolDefault.Publish(userId);
        gradeYear.Publish(userId);
        branchGradeYear.Publish(userId);

        var applicable = InterviewAssessmentPolicyCatalog.ResolveApplicable(
            [schoolDefault, gradeYear, branchGradeYear],
            branchId,
            stageId,
            gradeId,
            yearId);

        Assert.NotNull(applicable);
        Assert.Equal(branchGradeYear.Id, applicable!.Id);
    }

    [Fact]
    public void NotRequired_ClearsOperationalFields()
    {
        var policy = CreatePolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            requirementMode: InterviewAssessmentRequirementMode.InterviewOnly,
            deliveryMode: InterviewAssessmentDeliveryMode.Online,
            meetingProviderCode: "Simulated");

        policy.UpdateDraft(
            InterviewAssessmentRequirementMode.NotRequired,
            deliveryMode: InterviewAssessmentDeliveryMode.OnSite,
            requiredParticipants: InterviewAssessmentRequiredParticipants.Both,
            expectedDurationMinutes: 60,
            bookingWindowOpensDaysBefore: 30,
            bookingWindowClosesDaysBefore: 7,
            minimumSchedulingLeadTimeHours: 24,
            parentReschedulingAllowed: true,
            maxParentRescheduleAttempts: 2,
            parentCancellationAllowed: true,
            preparationNotesAr: "تحضير",
            preparationNotesEn: "Prep",
            onSiteInstructionsAr: "موقع",
            onSiteInstructionsEn: "Site",
            onlineInstructionsAr: "أونلاين",
            onlineInstructionsEn: "Online",
            meetingProviderCode: "Simulated",
            hybridSelectionAuthority: HybridDeliverySelectionAuthority.ParentMayChoose,
            schoolBranchId: null,
            educationalStageId: null,
            gradeId: null,
            academicYearId: null,
            updatedByUserId: Guid.NewGuid());

        Assert.Equal(InterviewAssessmentRequirementMode.NotRequired, policy.RequirementMode);
        Assert.Null(policy.DeliveryMode);
        Assert.Null(policy.RequiredParticipants);
        Assert.Null(policy.ExpectedDurationMinutes);
        Assert.Null(policy.MeetingProviderCode);
        Assert.Null(policy.OnSiteInstructionsAr);
        Assert.Null(policy.OnlineInstructionsAr);
        Assert.Equal("تحضير", policy.PreparationNotesAr);
        Assert.Equal(0, policy.MaxParentRescheduleAttempts);
        Assert.False(policy.ParentReschedulingAllowed);
    }

    [Fact]
    public void NotRequired_EnumDefinesOnlyDocumentedValues()
    {
        var modes = Enum.GetValues<InterviewAssessmentRequirementMode>()
            .OrderBy(value => (int)value)
            .ToArray();

        Assert.Equal(
            [
                InterviewAssessmentRequirementMode.NotRequired,
                InterviewAssessmentRequirementMode.InterviewOnly,
                InterviewAssessmentRequirementMode.AssessmentOnly,
                InterviewAssessmentRequirementMode.InterviewAndAssessment,
            ],
            modes);
        Assert.Equal(1, (int)InterviewAssessmentRequirementMode.NotRequired);
    }

    [Fact]
    public void MeetingIntegration_AllowsSimulatedAndDevelopment_RejectsArbitrary()
    {
        var validator = new IntegrationSettingsValidator();

        Assert.True(validator.Validate(
            IntegrationType.Meeting, IntegrationProviderCodes.Simulated, "{}", 1).IsValid);
        Assert.True(validator.Validate(
            IntegrationType.Meeting, IntegrationProviderCodes.Development, "{}", 1).IsValid);

        var rejected = validator.Validate(IntegrationType.Meeting, "Zoom", "{}", 1);
        Assert.False(rejected.IsValid);
        Assert.Contains("integrations.providerNotConfigured", rejected.ErrorCodes);
    }

    [Fact]
    public void ValidateForPublish_Online_BlockedWithoutMeeting()
    {
        var policy = CreateOperationalPolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InterviewAssessmentDeliveryMode.Online,
            meetingProviderCode: "Simulated");

        var code = SchoolInterviewAssessmentPolicyMapping.ValidateForPublish(
            policy,
            branch: null,
            hasActiveMeetingProvider: false,
            meetingProviderCodes: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Equal(SchoolPortalErrorCodes.InterviewAssessmentPolicyMeetingUnavailable, code);
    }

    [Fact]
    public void ValidateForPublish_OnSite_RequiresBranchAddress()
    {
        var schoolId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var policy = CreateOperationalPolicy(
            schoolId,
            userId,
            InterviewAssessmentDeliveryMode.OnSite,
            branchId: branchId,
            yearId: yearId);

        var branchWithoutAddress = new SchoolBranch(
            schoolId,
            "فرع",
            "Branch",
            "branch-no-address",
            Guid.NewGuid(),
            Guid.NewGuid(),
            isMainBranch: false);

        var code = SchoolInterviewAssessmentPolicyMapping.ValidateForPublish(
            policy,
            branchWithoutAddress,
            hasActiveMeetingProvider: true,
            meetingProviderCodes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                IntegrationProviderCodes.Simulated,
            });

        Assert.Equal(SchoolPortalErrorCodes.InterviewAssessmentPolicyOnSiteBranchInvalid, code);
    }

    [Fact]
    public void ValidateForPublish_Hybrid_RequiresSelectionAuthority()
    {
        var schoolId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var policy = CreateOperationalPolicy(
            schoolId,
            userId,
            InterviewAssessmentDeliveryMode.Hybrid,
            branchId: branchId,
            yearId: yearId,
            meetingProviderCode: IntegrationProviderCodes.Simulated,
            hybridSelectionAuthority: null);

        var branch = new SchoolBranch(
            schoolId,
            "فرع",
            "Branch",
            "branch-hybrid",
            Guid.NewGuid(),
            Guid.NewGuid(),
            isMainBranch: false);
        branch.UpdateAddress(
            "شارع الاختبار",
            "Test Street",
            buildingNumber: null,
            streetName: null,
            landmark: null,
            postalCode: null,
            addressReference: null,
            latitude: null,
            longitude: null);

        var code = SchoolInterviewAssessmentPolicyMapping.ValidateForPublish(
            policy,
            branch,
            hasActiveMeetingProvider: true,
            meetingProviderCodes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                IntegrationProviderCodes.Simulated,
            });

        Assert.Equal(SchoolPortalErrorCodes.InterviewAssessmentPolicyPublishInvalid, code);
    }

    [Fact]
    public void Snapshot_FromPolicy_ExcludesSecrets_KeepsMeetingProviderCode()
    {
        var policy = CreateOperationalPolicy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InterviewAssessmentDeliveryMode.Online,
            meetingProviderCode: IntegrationProviderCodes.Simulated);

        var snapshot = AdmissionApplicationInterviewAssessmentPolicySnapshot.FromPolicy(
            Guid.NewGuid(),
            policy,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Equal(IntegrationProviderCodes.Simulated, snapshot.MeetingProviderCode);
        Assert.Null(typeof(AdmissionApplicationInterviewAssessmentPolicySnapshot).GetProperty("SettingsJson"));
        Assert.Null(typeof(AdmissionApplicationInterviewAssessmentPolicySnapshot).GetProperty("MeetingUrl"));
        Assert.Null(typeof(AdmissionApplicationInterviewAssessmentPolicySnapshot).GetProperty("Credentials"));
    }

    [Fact]
    public async Task SchoolPortalPolicies_Parent_Returns403()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword);

        var profile = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        profile.EnsureSuccessStatusCode();
        var schoolId = (await profile.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.GetAsync(
            $"/api/school-portal/schools/{schoolId}/interview-assessment-policies");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePolicy_WithoutCsrf_Returns400()
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

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/interview-assessment-policies",
            new
            {
                requirementMode = (int)InterviewAssessmentRequirementMode.NotRequired,
                parentReschedulingAllowed = false,
                maxParentRescheduleAttempts = 0,
                parentCancellationAllowed = false,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Publish_EqualScopeKey_ReturnsConflict()
    {
        await AdmissionTestHelpers.MutatingGate.WaitAsync();
        try
        {
            await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);

            Guid schoolId;
            Guid yearId;
            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
                schoolId = await db.Schools
                    .Where(school => school.Slug == AuthTestHelpers.DemoSchoolSlug)
                    .Select(school => school.Id)
                    .SingleAsync();
                yearId = await db.AcademicYears
                    .Where(year => year.IsActive)
                    .OrderByDescending(year => year.IsCurrent)
                    .Select(year => year.Id)
                    .FirstAsync();
            }

            using var client = AuthTestHelpers.CreateCookieClient(_factory);
            Assert.True(await AuthTestHelpers.TryLoginAsync(
                client,
                AuthTestHelpers.SchoolOwnerEmail,
                AuthTestHelpers.DefaultPassword));

            var basePath = $"/api/school-portal/schools/{schoolId}/interview-assessment-policies";
            var draftBody = new
            {
                requirementMode = (int)InterviewAssessmentRequirementMode.NotRequired,
                parentReschedulingAllowed = false,
                maxParentRescheduleAttempts = 0,
                parentCancellationAllowed = false,
                academicYearId = yearId,
            };

            var firstCreate = await client.PostAsJsonAsync(basePath, draftBody);
            firstCreate.EnsureSuccessStatusCode();
            var firstId = (await firstCreate.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").GetProperty("id").GetGuid();

            var firstPublish = await client.PostAsync($"{basePath}/{firstId}/publish", null);
            firstPublish.EnsureSuccessStatusCode();

            var secondCreate = await client.PostAsJsonAsync(basePath, draftBody);
            secondCreate.EnsureSuccessStatusCode();
            var secondId = (await secondCreate.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").GetProperty("id").GetGuid();

            var secondPublish = await client.PostAsync($"{basePath}/{secondId}/publish", null);
            Assert.Equal(HttpStatusCode.BadRequest, secondPublish.StatusCode);
            var conflictJson = await secondPublish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(
                SchoolPortalErrorCodes.InterviewAssessmentPolicyConflict,
                conflictJson.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()));

            await client.PostAsync($"{basePath}/{firstId}/unpublish", null);
            await client.PostAsync($"{basePath}/{firstId}/deactivate", null);
            await client.PostAsync($"{basePath}/{secondId}/deactivate", null);
        }
        finally
        {
            AdmissionTestHelpers.MutatingGate.Release();
        }
    }

    private static SchoolInterviewAssessmentPolicy CreateOperationalPolicy(
        Guid schoolId,
        Guid userId,
        InterviewAssessmentDeliveryMode deliveryMode,
        Guid? branchId = null,
        Guid? yearId = null,
        string? meetingProviderCode = null,
        HybridDeliverySelectionAuthority? hybridSelectionAuthority = null)
    {
        var needsOnSite = deliveryMode is InterviewAssessmentDeliveryMode.OnSite
            or InterviewAssessmentDeliveryMode.Hybrid;
        var needsOnline = deliveryMode is InterviewAssessmentDeliveryMode.Online
            or InterviewAssessmentDeliveryMode.Hybrid;

        return new SchoolInterviewAssessmentPolicy(
            schoolId,
            InterviewAssessmentRequirementMode.InterviewOnly,
            deliveryMode,
            InterviewAssessmentRequiredParticipants.Child,
            expectedDurationMinutes: 30,
            bookingWindowOpensDaysBefore: null,
            bookingWindowClosesDaysBefore: null,
            minimumSchedulingLeadTimeHours: 24,
            parentReschedulingAllowed: false,
            maxParentRescheduleAttempts: 0,
            parentCancellationAllowed: false,
            preparationNotesAr: "تحضير",
            preparationNotesEn: "Prep",
            onSiteInstructionsAr: needsOnSite ? "تعليمات الموقع" : null,
            onSiteInstructionsEn: needsOnSite ? "On-site instructions" : null,
            onlineInstructionsAr: needsOnline ? "تعليمات الأونلاين" : null,
            onlineInstructionsEn: needsOnline ? "Online instructions" : null,
            meetingProviderCode: needsOnline ? meetingProviderCode : null,
            hybridSelectionAuthority,
            branchId,
            educationalStageId: null,
            gradeId: null,
            academicYearId: yearId,
            userId);
    }

    private static SchoolInterviewAssessmentPolicy CreatePolicy(
        Guid schoolId,
        Guid userId,
        Guid? yearId = null,
        Guid? branchId = null,
        Guid? stageId = null,
        Guid? gradeId = null,
        InterviewAssessmentRequirementMode requirementMode = InterviewAssessmentRequirementMode.NotRequired,
        InterviewAssessmentDeliveryMode? deliveryMode = null,
        string? meetingProviderCode = null) =>
        new(
            schoolId,
            requirementMode,
            deliveryMode,
            requiredParticipants: requirementMode == InterviewAssessmentRequirementMode.NotRequired
                ? null
                : InterviewAssessmentRequiredParticipants.Child,
            expectedDurationMinutes: requirementMode == InterviewAssessmentRequirementMode.NotRequired ? null : 30,
            bookingWindowOpensDaysBefore: null,
            bookingWindowClosesDaysBefore: null,
            minimumSchedulingLeadTimeHours: requirementMode == InterviewAssessmentRequirementMode.NotRequired ? null : 24,
            parentReschedulingAllowed: false,
            maxParentRescheduleAttempts: 0,
            parentCancellationAllowed: false,
            preparationNotesAr: null,
            preparationNotesEn: null,
            onSiteInstructionsAr: null,
            onSiteInstructionsEn: null,
            onlineInstructionsAr: null,
            onlineInstructionsEn: null,
            meetingProviderCode,
            hybridSelectionAuthority: null,
            branchId,
            stageId,
            gradeId,
            yearId,
            userId);
}
