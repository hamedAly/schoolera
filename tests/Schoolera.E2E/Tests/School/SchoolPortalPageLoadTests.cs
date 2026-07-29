using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class SchoolPortalPageLoadTests : SchoolOwnerAuthTest
{
    private static string? _schoolId;

    public static TheoryData<string> PortalSuffixes =>
    [
        "overview",
        "profile",
        "branches",
        "stages",
        "fees",
        "facilities",
        "gallery",
        "services",
        "team",
        "applications",
        "admission-requirements",
        "admission-questions",
        "age-eligibility-rules",
        "interview-assessment-policies",
        "interview-assessment-slots",
        "interview-faqs",
    ];

    [Theory]
    [MemberData(nameof(PortalSuffixes))]
    public async Task SchoolPortalRoute_LoadsWithoutCrash(string suffix)
    {
        _schoolId ??= await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{_schoolId}/{suffix}");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }

    [Fact]
    public async Task SchoolOnboarding_Wizard_Loads()
    {
        await GotoAsync("/school/onboarding");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }

    [Fact]
    public async Task SchoolOnboarding_Status_Loads()
    {
        await GotoAsync("/school/onboarding/status");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }
}
