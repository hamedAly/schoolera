using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Support;

public sealed class SupportMutationTests : SupportAuthTest
{
    [Fact]
    public async Task Support_Tickets_List_AllowsOpeningDetailWhenPresent()
    {
        await GotoAsync("/support/tickets");
        await ExpectMainOrHeadingAsync();

        var link = Page.Locator("a[href*='/support/tickets/']").First;
        if (await link.CountAsync() > 0 && await link.IsVisibleAsync())
        {
            await link.ClickAsync();
            await ExpectMainOrHeadingAsync();
            await ExpectPageHealthyAsync();
        }
    }
}
