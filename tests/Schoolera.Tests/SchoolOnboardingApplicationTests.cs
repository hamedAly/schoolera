using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

public sealed class SchoolOnboardingApplicationTests
{
    [Fact]
    public void NewApplication_StartsAsEditableDraftOnOrganizationStep()
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());

        Assert.Equal(SchoolOnboardingStatus.Draft, application.Status);
        Assert.Equal(SchoolOnboardingStep.Organization, application.CurrentStep);
        Assert.True(application.IsEditable);
    }

    [Theory]
    [InlineData(SchoolOnboardingStatus.Draft, true)]
    [InlineData(SchoolOnboardingStatus.ChangesRequested, true)]
    [InlineData(SchoolOnboardingStatus.Submitted, false)]
    [InlineData(SchoolOnboardingStatus.UnderReview, false)]
    [InlineData(SchoolOnboardingStatus.Approved, false)]
    [InlineData(SchoolOnboardingStatus.Rejected, false)]
    public void IsEditable_OnlyForDraftAndChangesRequested(SchoolOnboardingStatus status, bool expected)
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());

        var reviewer = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        switch (status)
        {
            case SchoolOnboardingStatus.Submitted:
                application.Submit(now);
                break;
            case SchoolOnboardingStatus.UnderReview:
                application.Submit(now);
                application.StartReview(reviewer, now);
                break;
            case SchoolOnboardingStatus.ChangesRequested:
                application.Submit(now);
                application.StartReview(reviewer, now);
                application.RequestChanges(reviewer, now);
                break;
            case SchoolOnboardingStatus.Approved:
                application.Submit(now);
                application.StartReview(reviewer, now);
                application.Approve(reviewer, Guid.NewGuid(), now);
                break;
            case SchoolOnboardingStatus.Rejected:
                application.Submit(now);
                application.StartReview(reviewer, now);
                application.Reject(reviewer, now);
                break;
        }

        Assert.Equal(expected, application.IsEditable);
    }

    [Fact]
    public void SaveOrganization_NormalizesRegistrationAndCountry()
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());

        application.SaveOrganization(
            organizationNameAr: "  مدرسة  ",
            organizationNameEn: "School",
            legalName: null,
            countryCode: "eg",
            registrationOrLicenseNumber: "  ab-123  ",
            taxRegistrationNumber: null,
            legalForm: null,
            organizationAddress: null,
            organizationWebsite: null);

        Assert.Equal("مدرسة", application.OrganizationNameAr);
        Assert.Equal("EG", application.CountryCode);
        Assert.Equal("ab-123", application.RegistrationOrLicenseNumber);
        Assert.Equal("AB-123", application.NormalizedRegistrationNumber);
    }

    [Fact]
    public void MarkStepReached_OnlyAdvancesForwardWhileEditable()
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());

        application.MarkStepReached(SchoolOnboardingStep.SchoolDetails);
        Assert.Equal(SchoolOnboardingStep.SchoolDetails, application.CurrentStep);

        // Does not move backwards.
        application.MarkStepReached(SchoolOnboardingStep.Organization);
        Assert.Equal(SchoolOnboardingStep.SchoolDetails, application.CurrentStep);
    }

    [Fact]
    public void MarkStepReached_DoesNotAdvanceWhenNotEditable()
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());
        application.Submit(DateTimeOffset.UtcNow);

        application.MarkStepReached(SchoolOnboardingStep.Documents);

        Assert.Equal(SchoolOnboardingStep.Review, application.CurrentStep);
    }

    [Fact]
    public void WorkflowTransitions_SetTimestampsAndActors()
    {
        var reviewer = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var application = new SchoolOnboardingApplication(Guid.NewGuid());

        application.Submit(now);
        Assert.Equal(SchoolOnboardingStatus.Submitted, application.Status);
        Assert.Equal(now, application.SubmittedAtUtc);

        application.StartReview(reviewer, now);
        Assert.Equal(SchoolOnboardingStatus.UnderReview, application.Status);
        Assert.Equal(reviewer, application.ReviewedByUserId);

        application.RequestChanges(reviewer, now);
        Assert.Equal(SchoolOnboardingStatus.ChangesRequested, application.Status);
        Assert.Equal(SchoolOnboardingStep.Organization, application.CurrentStep);

        application.Resubmit(now);
        Assert.Equal(SchoolOnboardingStatus.Submitted, application.Status);
        Assert.Equal(now, application.LastResubmittedAtUtc);

        application.StartReview(reviewer, now);
        application.Approve(reviewer, schoolId, now);
        Assert.Equal(SchoolOnboardingStatus.Approved, application.Status);
        Assert.Equal(schoolId, application.ApprovedSchoolId);
        Assert.Equal(now, application.ApprovedAtUtc);
    }

    [Fact]
    public void AddDocument_TracksCurrentAndRetirePreservesAudit()
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());
        var typeId = Guid.NewGuid();
        var uploader = Guid.NewGuid();

        var document = new SchoolOnboardingDocument(
            application.Id, typeId, "cr.pdf", "ref/cr.pdf", "application/pdf", 1234, "hash", uploader);
        application.AddDocument(document);

        Assert.Single(application.Documents);
        Assert.True(document.IsCurrent);

        var now = DateTimeOffset.UtcNow;
        document.Retire(now);

        Assert.False(document.IsCurrent);
        Assert.Equal(now, document.ReplacedAtUtc);
    }
}
