using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Parent;

public sealed class JourneyBParentTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_Dashboard_Children_And_Applications()
    {
        await GotoAsync("/parent/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent(/dashboard)?.*"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        await GotoAsync("/parent/children");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/children.*"));
        await ExpectMainOrHeadingAsync();
        await WaitForPortalListReadyAsync();

        // Seed parent has two children with masked identity.
        var childRows = Page.Locator("table tbody tr");
        var childCards = Page.Locator(".parent-child-card");
        var rowCount = await childRows.CountAsync();
        var cardCount = await childCards.CountAsync();
        var childCount = Math.Max(rowCount, cardCount);
        Assert.True(childCount >= 2, $"Expected at least 2 seeded children, found {childCount}.");
        await Expect(Page.Locator(".parent-readonly-field").First).ToBeVisibleAsync();

        var editLink = Page.Locator("a[href*='/parent/children/'][href$='/edit']").First;
        await Expect(editLink).ToBeVisibleAsync();
        await editLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/children/.+/edit.*"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        await GotoAsync("/parent/applications");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/applications.*"));
        await ExpectMainOrHeadingAsync();

        var appLink = Page.Locator("a[href*='/parent/applications/']:not([href$='/new']):not([href$='/edit'])").First;
        if (await appLink.CountAsync() > 0 && await appLink.IsVisibleAsync())
        {
            await appLink.ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/applications/.+"));
            await ExpectMainOrHeadingAsync();
            await ExpectPageHealthyAsync();
        }
    }
}
