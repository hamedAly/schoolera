using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Schoolera.E2E.Infrastructure;

/// <summary>
/// Base Playwright page test with Schoolera base URL and default timeouts.
/// </summary>
public abstract class SchooleraPageTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = E2EConfig.BaseUrl.TrimEnd('/') + "/",
            Locale = "ar-EG",
            IgnoreHTTPSErrors = true,
            RecordVideoDir = null,
        };
    }

    protected async Task GotoAsync(string relativePath, WaitUntilState? waitUntil = null)
    {
        var path = relativePath.TrimStart('/');
        var until = waitUntil ?? (path.StartsWith("schools", StringComparison.OrdinalIgnoreCase)
            ? WaitUntilState.Load
            : WaitUntilState.NetworkIdle);

        Page.SetDefaultTimeout(E2EConfig.DefaultTimeoutMs);
        await Page.GotoAsync(path, new PageGotoOptions
        {
            WaitUntil = until,
            Timeout = E2EConfig.NavigationTimeoutMs,
        });
    }

    protected async Task WaitForPortalListReadyAsync()
    {
        var skeleton = Page.Locator("se-portal-loading-skeleton");
        if (await skeleton.CountAsync() > 0)
        {
            await skeleton.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Detached,
                Timeout = E2EConfig.NavigationTimeoutMs,
            });
        }
    }

    protected async Task ExpectPageHealthyAsync()
    {
        await Expect(Page.Locator("body")).ToBeVisibleAsync();
        await Expect(Page.Locator("se-global-error, .global-error-shell")).ToHaveCountAsync(0);
    }

    protected async Task ExpectMainOrHeadingAsync()
    {
        var main = Page.Locator("main, [role='main'], h1, se-page-hero, .parent-page, .admin-page, .portal-page, .contact-page, .school-search");
        await Expect(main.First).ToBeVisibleAsync();
    }
}
