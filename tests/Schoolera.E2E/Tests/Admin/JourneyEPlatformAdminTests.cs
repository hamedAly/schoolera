using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Admin;

public sealed class JourneyEPlatformAdminTests : AdminAuthTest
{
    [Theory]
    [InlineData("/admin/dashboard")]
    [InlineData("/admin/onboarding")]
    [InlineData("/admin/applications")]
    [InlineData("/admin/schools")]
    [InlineData("/admin/users")]
    [InlineData("/admin/taxonomies")]
    [InlineData("/admin/cms/pages")]
    [InlineData("/admin/cms/faq")]
    [InlineData("/admin/cms/home")]
    [InlineData("/admin/contact-requests")]
    [InlineData("/admin/audit")]
    public async Task PlatformAdmin_CorePages_Load(string path)
    {
        await GotoAsync(path);
        await Expect(Page).ToHaveURLAsync(new Regex($".*{Regex.Escape(path)}.*"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task PlatformAdmin_ContactDemo_VisibleAndDetailOpens()
    {
        await GotoAsync("/admin/contact-requests");
        await ExpectMainOrHeadingAsync();

        // Prefer demo row if seeded, but don't hard-fail if seed content varies.
        var detailLink = Page.Locator("a[href*='/admin/contact-requests/']").First;
        Assert.True(await detailLink.CountAsync() > 0, "Expected at least one contact request detail link.");

        await detailLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/admin/contact-requests/.+"));
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task PlatformAdmin_SchoolsList_OpensDetailWhenPresent()
    {
        await GotoAsync("/admin/schools");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator("a[href*='/admin/schools/']").First;
        if (await detailLink.CountAsync() > 0 && await detailLink.IsVisibleAsync())
        {
            await detailLink.ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex(".*/admin/schools/.+"));
            await ExpectMainOrHeadingAsync();
            await ExpectPageHealthyAsync();
        }
    }
}
