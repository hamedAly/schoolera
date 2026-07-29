using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class JourneyDAdmissionReviewTests : SchoolAdminAuthTest
{
    [Fact]
    public async Task SchoolAdmin_Applications_List_Detail_And_Actions()
    {
        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);

        await GotoAsync($"/school/{schoolId}/applications");
        await Expect(Page).ToHaveURLAsync(new Regex($".*/school/{schoolId}/applications.*"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
        await WaitForPortalListReadyAsync();

        var detailLink = Page.Locator($"a[href*='/school/{schoolId}/applications/']").First;
        Assert.True(await detailLink.CountAsync() > 0, "Expected at least one seeded admission application link.");
        await detailLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($".*/school/{schoolId}/applications/.+"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        await Expect(Page.Locator(".portal-application-detail, .portal-application-detail__head, se-portal-admission-status-badge").First)
            .ToBeVisibleAsync();

        var actions = Page.Locator(
            "[data-testid='admission-start-review'], [data-testid='admission-accept'], [data-testid='admission-reject']");
        if (await actions.CountAsync() > 0)
        {
            await Expect(actions.First).ToBeVisibleAsync();
        }
    }
}

/// <summary>
/// Parent visibility of admission status after school review data exists (seed fixtures).
/// </summary>
public sealed class JourneyDParentVisibilityTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_ApplicationDetail_ShowsStatusWithoutInternalNotes()
    {
        await GotoAsync("/parent/applications");
        await ExpectMainOrHeadingAsync();

        var appLink = Page.Locator("a[href*='/parent/applications/']:not([href$='/new']):not([href$='/edit'])").First;
        if (await appLink.CountAsync() == 0 || !await appLink.IsVisibleAsync())
        {
            return;
        }

        await appLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/applications/.+"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        // Internal school notes must not appear in parent UI.
        await Expect(Page.GetByText("internalReviewNote", new PageGetByTextOptions { Exact = false })).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Internal note", new PageGetByTextOptions { Exact = false })).ToHaveCountAsync(0);
    }
}
