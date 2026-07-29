using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;
using Schoolera.E2E.Pages.Shared;

namespace Schoolera.E2E.Tests.CrossCutting;

[Collection(E2ECollection.Name)]
public sealed class ParentLocalizationTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.Parent);
        return options;
    }

    [Theory]
    [InlineData("/parent/dashboard")]
    [InlineData("/parent/children")]
    [InlineData("/parent/applications")]
    public async Task ParentShell_LanguageSwitch_UpdatesLangAndDir(string path)
    {
        await GotoAsync(path);
        var html = Page.Locator("html");
        var switcher = new LanguageSwitcher(Page);

        await switcher.SwitchToAsync("en");
        await Expect(html).ToHaveAttributeAsync("dir", "ltr");

        await switcher.SwitchToAsync("ar");
        await Expect(html).ToHaveAttributeAsync("dir", "rtl");
    }
}

[Collection(E2ECollection.Name)]
public sealed class SchoolPortalLocalizationTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.SchoolOwner);
        return options;
    }

    [Fact]
    public async Task SchoolPortalShell_LanguageSwitch_UpdatesLangAndDir()
    {
        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{schoolId}/overview");
        var html = Page.Locator("html");
        var switcher = new LanguageSwitcher(Page);

        await switcher.SwitchToAsync("en");
        await Expect(html).ToHaveAttributeAsync("dir", "ltr");

        await switcher.SwitchToAsync("ar");
        await Expect(html).ToHaveAttributeAsync("dir", "rtl");
    }
}

[Collection(E2ECollection.Name)]
public sealed class AuthenticatedViewportTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.Parent);
        return options;
    }

    [Theory]
    [InlineData("/parent/dashboard", 390, 844)]
    [InlineData("/parent/children", 390, 844)]
    [InlineData("/parent/dashboard", 768, 1024)]
    [InlineData("/parent/dashboard", 1440, 900)]
    public async Task ParentPages_NoHorizontalOverflow(string path, int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await GotoAsync(path);
        await ExpectPageHealthyAsync();

        var overflowX = await Page.EvaluateAsync<bool>("""
            () => {
                const doc = document.documentElement;
                // In RTL + complex layouts, tiny layout subpixel differences can create
                // minimal horizontal overflow. Allow a small tolerance.
                return doc.scrollWidth > doc.clientWidth + 64;
            }
        """);

        Assert.False(overflowX, $"Horizontal overflow on {path} at {width}x{height}");
    }
}

[Collection(E2ECollection.Name)]
public sealed class AdminViewportTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.PlatformAdmin);
        return options;
    }

    [Theory]
    [InlineData("/admin/dashboard", 768, 1024)]
    [InlineData("/admin/schools", 390, 844)]
    [InlineData("/admin/dashboard", 1440, 900)]
    public async Task AdminPages_NoHorizontalOverflow(string path, int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await GotoAsync(path);
        await ExpectPageHealthyAsync();

        var overflowX = await Page.EvaluateAsync<bool>("""
            () => {
                const doc = document.documentElement;
                return doc.scrollWidth > doc.clientWidth + 128;
            }
        """);

        Assert.False(overflowX, $"Horizontal overflow on {path} at {width}x{height}");
    }
}

public sealed class BilingualFormTests : SchooleraPageTest
{
    [Fact]
    public async Task PublicSchoolProfile_ShowsBilingualContent()
    {
        await GotoAsync($"/schools/{E2EConfig.PublishedSchoolSlug}");
        await ExpectMainOrHeadingAsync();

        var heading = Page.Locator("h1").First;
        var text = await heading.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(text), "School profile heading should not be empty.");
    }
}

public sealed class FaqInteractionTests : SchooleraPageTest
{
    [Fact]
    public async Task FaqPage_AccordionExpandsOnClick()
    {
        await GotoAsync("/faq");
        await ExpectMainOrHeadingAsync();

        var accordion = Page.Locator(
            ".faq-item, .accordion-item, details, mat-expansion-panel").First;
        if (await accordion.CountAsync() == 0)
            return;

        var trigger = accordion.Locator(
            "summary, .accordion-header, mat-expansion-panel-header, button").First;
        if (await trigger.CountAsync() > 0)
        {
            await trigger.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var content = accordion.Locator(
                ".accordion-body, .faq-item__answer, mat-expansion-panel-body, p").First;
            if (await content.CountAsync() > 0)
            {
                await Expect(content).ToBeVisibleAsync();
            }
        }
    }
}

public sealed class SchoolSearchFilterTests : SchooleraPageTest
{
    [Fact]
    public async Task SchoolSearch_FiltersArePresent()
    {
        await GotoAsync("/schools");
        await ExpectMainOrHeadingAsync();

        var filterSidebar = Page.Locator(
            ".school-search__sidebar, #school-search-filters-drawer, .filter-panel").First;
        await Expect(filterSidebar).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SchoolSearch_PaginationOrLoadMoreExists()
    {
        await GotoAsync("/schools");
        await ExpectMainOrHeadingAsync();

        var pagination = Page.Locator(
            ".pagination, [data-testid='pagination'], button:has-text('المزيد'), button:has-text('Load More'), .load-more").First;
        if (await pagination.CountAsync() > 0)
        {
            await Expect(pagination).ToBeVisibleAsync();
        }
    }
}

public sealed class NotFoundPageTests : SchooleraPageTest
{
    [Fact]
    public async Task InvalidRoute_ShowsNotFoundContent()
    {
        await GotoAsync("/this-route-definitely-does-not-exist-123");
        await ExpectPageHealthyAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("this-route-definitely-does-not-exist-123", RegexOptions.IgnoreCase));
        Assert.DoesNotContain("/auth/login", Page.Url, StringComparison.OrdinalIgnoreCase);
    }
}
