using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;
using Schoolera.E2E.Pages.Public;
using Schoolera.E2E.Pages.Shared;

namespace Schoolera.E2E.Tests.Public;

public sealed class JourneyAVisitorTests : SchooleraPageTest
{
    [Fact]
    public async Task Visitor_Home_Schools_Profile_Contact_And_LanguageSwitch()
    {
        var home = new HomePage(Page);
        await home.GotoAsync();
        await Expect(home.Heading).ToBeVisibleAsync();
        await ExpectPageHealthyAsync();

        var schools = new SchoolsListPage(Page);
        await schools.GotoAsync();
        await Expect(schools.Heading).ToBeVisibleAsync();
        await Expect(Page.Locator(".school-search__sidebar, #school-search-filters-drawer")).ToBeVisibleAsync();
        await schools.SearchAsync("cairo");
        await Expect(schools.SearchInput).ToHaveValueAsync("cairo");
        await Expect(Page.Locator(".school-search__results, .school-search__grid, a[href*='/schools/']").First)
            .ToBeVisibleAsync();

        var profile = new SchoolProfilePage(Page);
        await profile.GotoAsync(E2EConfig.PublishedSchoolSlug);
        await Expect(profile.Heading).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($".*/schools/{E2EConfig.PublishedSchoolSlug}.*"));
        await Expect(Page.Locator("#school-fees-heading, #school-gallery-heading, #school-overview-heading").First)
            .ToBeVisibleAsync();

        var contact = new ContactPage(Page);
        await contact.GotoAsync();
        await Expect(contact.Heading).ToBeVisibleAsync();

        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await contact.FillValidAsync(suffix);
        await contact.Submit.ClickAsync();
        await Expect(contact.Success).ToBeVisibleAsync();

        var switcher = new LanguageSwitcher(Page);
        var html = Page.Locator("html");
        var beforeLang = await html.GetAttributeAsync("lang") ?? "ar";
        var beforeDir = await html.GetAttributeAsync("dir") ?? "rtl";
        await switcher.SwitchAsync();
        if (beforeLang.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            await Expect(html).ToHaveAttributeAsync("lang", new Regex("^en", RegexOptions.IgnoreCase));
            await Expect(html).ToHaveAttributeAsync("dir", "ltr");
        }
        else
        {
            await Expect(html).ToHaveAttributeAsync("lang", new Regex("^ar", RegexOptions.IgnoreCase));
            await Expect(html).ToHaveAttributeAsync("dir", "rtl");
        }

        Assert.NotEqual(beforeDir, await html.GetAttributeAsync("dir"));
    }
}
