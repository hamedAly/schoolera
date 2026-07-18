using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Schoolera.Application.Admissions.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class AdmissionLifecycleTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public AdmissionLifecycleTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.MissingDocuments, true)]
    [InlineData(AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.InterviewRequired, true)]
    [InlineData(AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.AssessmentRequired, true)]
    [InlineData(AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.WaitingList, true)]
    [InlineData(AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.Accepted, true)]
    [InlineData(AdmissionApplicationStatus.UnderReview, AdmissionApplicationStatus.Rejected, true)]
    [InlineData(AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.UnderReview, true)]
    [InlineData(AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.WaitingList, true)]
    [InlineData(AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.Accepted, true)]
    [InlineData(AdmissionApplicationStatus.InterviewRequired, AdmissionApplicationStatus.Rejected, true)]
    [InlineData(AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.UnderReview, true)]
    [InlineData(AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.WaitingList, true)]
    [InlineData(AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.Accepted, true)]
    [InlineData(AdmissionApplicationStatus.AssessmentRequired, AdmissionApplicationStatus.Rejected, true)]
    [InlineData(AdmissionApplicationStatus.WaitingList, AdmissionApplicationStatus.UnderReview, true)]
    [InlineData(AdmissionApplicationStatus.WaitingList, AdmissionApplicationStatus.Accepted, true)]
    [InlineData(AdmissionApplicationStatus.WaitingList, AdmissionApplicationStatus.Rejected, true)]
    [InlineData(AdmissionApplicationStatus.Accepted, AdmissionApplicationStatus.Registered, true)]
    [InlineData(AdmissionApplicationStatus.Submitted, AdmissionApplicationStatus.Accepted, false)]
    [InlineData(AdmissionApplicationStatus.Accepted, AdmissionApplicationStatus.Rejected, false)]
    [InlineData(AdmissionApplicationStatus.Registered, AdmissionApplicationStatus.Accepted, false)]
    [InlineData(AdmissionApplicationStatus.Rejected, AdmissionApplicationStatus.UnderReview, false)]
    [InlineData(AdmissionApplicationStatus.Cancelled, AdmissionApplicationStatus.Submitted, false)]
    public void SchoolTransitionMatrix_MatchesPolicy(
        AdmissionApplicationStatus from,
        AdmissionApplicationStatus to,
        bool allowed)
    {
        var ok = AdmissionTransitionPolicy.TryValidateSchoolTransition(from, to, out _);
        Assert.Equal(allowed, ok);
    }

    [Fact]
    public void ParentResubmit_MissingDocuments_ToUnderReview_Allowed()
    {
        Assert.True(AdmissionTransitionPolicy.TryValidateParentTransition(
            AdmissionApplicationStatus.MissingDocuments,
            AdmissionApplicationStatus.UnderReview,
            reviewStartedAtUtc: DateTimeOffset.UtcNow,
            out _));
    }

    [Fact]
    public void ParentCancel_InterviewRequired_BlockedByExistingPolicy()
    {
        Assert.False(AdmissionTransitionPolicy.CanParentCancel(
            AdmissionApplicationStatus.InterviewRequired,
            DateTimeOffset.UtcNow));
        Assert.False(AdmissionTransitionPolicy.TryValidateParentTransition(
            AdmissionApplicationStatus.InterviewRequired,
            AdmissionApplicationStatus.Cancelled,
            DateTimeOffset.UtcNow,
            out var code));
        Assert.Equal("admission.application.cancellationNotAllowed", code);
    }

    [Fact]
    public void ActiveDuplicate_IncludesNewLifecycleStatuses()
    {
        Assert.True(AdmissionTransitionPolicy.IsActiveDuplicateStatus(AdmissionApplicationStatus.MissingDocuments));
        Assert.True(AdmissionTransitionPolicy.IsActiveDuplicateStatus(AdmissionApplicationStatus.InterviewRequired));
        Assert.True(AdmissionTransitionPolicy.IsActiveDuplicateStatus(AdmissionApplicationStatus.AssessmentRequired));
        Assert.True(AdmissionTransitionPolicy.IsActiveDuplicateStatus(AdmissionApplicationStatus.WaitingList));
        Assert.True(AdmissionTransitionPolicy.IsActiveDuplicateStatus(AdmissionApplicationStatus.Registered));
        Assert.False(AdmissionTransitionPolicy.IsActiveDuplicateStatus(AdmissionApplicationStatus.Rejected));
        Assert.False(AdmissionTransitionPolicy.IsActiveDuplicateStatus(AdmissionApplicationStatus.Cancelled));
    }

    [Fact]
    public async Task RequestMissingDocuments_WithoutCsrf_Returns400()
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
            $"/api/school-portal/schools/{schoolId}/applications/{Guid.NewGuid()}/request-missing-documents",
            new
            {
                parentVisibleReason = "Need documents",
                items = Array.Empty<object>(),
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LifecycleEndpoints_Parent_Returns403()
    {
        await AdmissionTestHelpers.EnsureDemoSchoolAdmissionReadyAsync(_factory);
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await AuthTestHelpers.TryLoginAsync(client, AuthTestHelpers.ParentEmail, AuthTestHelpers.DefaultPassword);

        var profile = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        profile.EnsureSuccessStatusCode();
        var schoolId = (await profile.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/school-portal/schools/{schoolId}/applications/{Guid.NewGuid()}/mark-registered",
            new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<Guid> GetDemoSchoolIdAsync(HttpClient client)
    {
        var profile = await client.GetAsync($"/api/schools/{AuthTestHelpers.DemoSchoolSlug}");
        profile.EnsureSuccessStatusCode();
        return (await profile.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();
    }
}
