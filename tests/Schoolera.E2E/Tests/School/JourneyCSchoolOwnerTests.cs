using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class JourneyCSchoolOwnerTests : SchoolOwnerAuthTest
{
    [Fact]
    public async Task SchoolOwner_Onboarding_Portal_And_Catalog()
    {
        await GotoAsync("/school/onboarding/status");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);
        Assert.False(string.IsNullOrWhiteSpace(schoolId));

        await GotoAsync($"/school/{schoolId}/overview");
        await Expect(Page).ToHaveURLAsync(new Regex($".*/school/{schoolId}/overview.*"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        foreach (var section in new[] { "branches", "stages", "fees", "gallery" })
        {
            await GotoAsync($"/school/{schoolId}/{section}");
            await Expect(Page).ToHaveURLAsync(new Regex($".*/school/{schoolId}/{section}.*"));
            await ExpectMainOrHeadingAsync();
            await ExpectPageHealthyAsync();
        }
    }
}
