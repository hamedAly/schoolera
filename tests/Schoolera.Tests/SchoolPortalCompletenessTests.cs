using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

public sealed class SchoolPortalCompletenessTests
{
    [Fact]
    public void CalculatePercent_AllChecksComplete_Returns100()
    {
        var counts = CreateCounts(allComplete: true);

        Assert.Equal(100, SchoolPortalCompleteness.CalculatePercent(counts));
    }

    [Fact]
    public void CalculatePercent_NoChecksComplete_Returns0()
    {
        var counts = CreateCounts(allComplete: false);

        Assert.Equal(0, SchoolPortalCompleteness.CalculatePercent(counts));
    }

    [Fact]
    public void CalculatePercent_HalfComplete_Returns50()
    {
        var counts = new SchoolDashboardCounts(
            NameAr: "مدرسة",
            NameEn: "School",
            Slug: "school",
            LogoUrl: "/logo.png",
            CoverUrl: "/cover.png",
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            BranchCount: 0,
            ActiveBranchCount: 0,
            MainBranchCount: 0,
            OfferingCount: 0,
            ActiveOfferingCount: 0,
            ActiveGradeCount: 0,
            TuitionFeeCount: 0,
            ActiveTuitionFeeCount: 0,
            FacilityCount: 0,
            ServiceCount: 0,
            ActiveServiceCount: 0,
            TeamMemberCount: 0,
            GalleryImageCount: 0,
            HasLogo: true,
            HasCover: true,
            HasShortDescription: true,
            HasFullDescription: true,
            HasPublicContact: true);

        // 5 of 10 checks complete.
        Assert.Equal(50, SchoolPortalCompleteness.CalculatePercent(counts));
    }

    [Fact]
    public void BuildWarnings_Suspended_IncludesSuspendedWarning()
    {
        var counts = CreateCounts(allComplete: true);

        var warnings = SchoolPortalCompleteness.BuildWarnings(SchoolStatus.Suspended, counts);

        Assert.Contains("schoolPortal.warnings.suspended", warnings);
    }

    [Fact]
    public void BuildWarnings_MissingAssets_IncludesExpectedKeys()
    {
        var counts = CreateCounts(allComplete: false);

        var warnings = SchoolPortalCompleteness.BuildWarnings(SchoolStatus.Published, counts);

        Assert.Contains("schoolPortal.warnings.missingLogo", warnings);
        Assert.Contains("schoolPortal.warnings.missingCover", warnings);
        Assert.Contains("schoolPortal.warnings.missingMainBranch", warnings);
        Assert.Contains("schoolPortal.warnings.noActiveOfferings", warnings);
        Assert.Contains("schoolPortal.warnings.noActiveTuitionFees", warnings);
        Assert.Contains("schoolPortal.warnings.missingPublicContact", warnings);
        Assert.DoesNotContain("schoolPortal.warnings.suspended", warnings);
    }

    [Fact]
    public void BuildWarnings_CompleteProfile_ReturnsEmptyForPublished()
    {
        var counts = CreateCounts(allComplete: true);

        var warnings = SchoolPortalCompleteness.BuildWarnings(SchoolStatus.Published, counts);

        Assert.Empty(warnings);
    }

    private static SchoolDashboardCounts CreateCounts(bool allComplete)
    {
        return new SchoolDashboardCounts(
            NameAr: "مدرسة",
            NameEn: "School",
            Slug: "school",
            LogoUrl: allComplete ? "/logo.png" : null,
            CoverUrl: allComplete ? "/cover.png" : null,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            BranchCount: allComplete ? 1 : 0,
            ActiveBranchCount: allComplete ? 1 : 0,
            MainBranchCount: allComplete ? 1 : 0,
            OfferingCount: allComplete ? 1 : 0,
            ActiveOfferingCount: allComplete ? 1 : 0,
            ActiveGradeCount: allComplete ? 1 : 0,
            TuitionFeeCount: allComplete ? 1 : 0,
            ActiveTuitionFeeCount: allComplete ? 1 : 0,
            FacilityCount: allComplete ? 1 : 0,
            ServiceCount: allComplete ? 1 : 0,
            ActiveServiceCount: allComplete ? 1 : 0,
            TeamMemberCount: allComplete ? 1 : 0,
            GalleryImageCount: allComplete ? 1 : 0,
            HasLogo: allComplete,
            HasCover: allComplete,
            HasShortDescription: allComplete,
            HasFullDescription: allComplete,
            HasPublicContact: allComplete);
    }
}
