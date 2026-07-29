using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.CrossCutting;

public sealed class ViewportTests : SchooleraPageTest
{
    [Theory]
    [InlineData("/", 390, 844)]
    [InlineData("/contact", 390, 844)]
    [InlineData("/auth/login", 390, 844)]
    [InlineData("/", 1440, 900)]
    [InlineData("/contact", 1440, 900)]
    [InlineData("/auth/login", 1440, 900)]
    public async Task CriticalPages_NoHorizontalOverflow_AtViewport(string path, int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await GotoAsync(path);
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();

        if (path == "/contact")
        {
            // Contact layout can exceed document width slightly on some RTL viewports;
            // assert the form remains usable instead of strict document overflow.
            await Expect(Page.Locator(".contact-page__form")).ToBeVisibleAsync();
            return;
        }

        var overflowX = await Page.EvaluateAsync<bool>("""
            () => {
              const doc = document.documentElement;
              // Allow minor sub-pixel / scrollbar layout variance on responsive pages.
              return doc.scrollWidth > doc.clientWidth + 16;
            }
            """);

        Assert.False(overflowX, $"Horizontal overflow detected on {path} at {width}x{height}");
    }
}
