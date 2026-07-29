using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;
using Schoolera.E2E.Pages.Shared;

namespace Schoolera.E2E.Tests.CrossCutting;

public sealed class LocalizationTests : SchooleraPageTest
{
    public static TheoryData<string> ShellRoutes =>
    [
        "/",
        "/schools",
        "/contact",
        "/auth/login",
        "/faq",
    ];

    [Theory]
    [MemberData(nameof(ShellRoutes))]
    public async Task PublicShell_LanguageSwitch_UpdatesLangAndDir(string path)
    {
        await GotoAsync(path);
        var html = Page.Locator("html");
        var switcher = new LanguageSwitcher(Page);

        // Default is Arabic RTL.
        await Expect(html).ToHaveAttributeAsync("lang", new Regex("^ar", RegexOptions.IgnoreCase));
        await Expect(html).ToHaveAttributeAsync("dir", "rtl");

        await switcher.SwitchAsync();
        await Expect(html).ToHaveAttributeAsync("lang", new Regex("^en", RegexOptions.IgnoreCase));
        await Expect(html).ToHaveAttributeAsync("dir", "ltr");

        await switcher.SwitchAsync();
        await Expect(html).ToHaveAttributeAsync("lang", new Regex("^ar", RegexOptions.IgnoreCase));
        await Expect(html).ToHaveAttributeAsync("dir", "rtl");
    }
}

[Collection(E2ECollection.Name)]
public sealed class AuthenticatedLocalizationTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.PlatformAdmin);
        return options;
    }

    [Fact]
    public async Task AdminShell_LanguageSwitch_UpdatesLangAndDir()
    {
        await GotoAsync("/admin/dashboard");
        var html = Page.Locator("html");
        var switcher = new LanguageSwitcher(Page);

        await switcher.SwitchToAsync("en");
        await Expect(html).ToHaveAttributeAsync("dir", "ltr");

        await switcher.SwitchToAsync("ar");
        await Expect(html).ToHaveAttributeAsync("dir", "rtl");
    }
}
