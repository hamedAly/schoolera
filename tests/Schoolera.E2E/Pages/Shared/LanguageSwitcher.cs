using Microsoft.Playwright;

namespace Schoolera.E2E.Pages.Shared;

public sealed class LanguageSwitcher(IPage page)
{
    public ILocator Button => page.Locator(".language-switcher__button").First;

    public async Task SwitchAsync()
    {
        await Button.ClickAsync();
    }

    public async Task SwitchToAsync(string lang)
    {
        var html = page.Locator("html");
        var current = await html.GetAttributeAsync("lang") ?? string.Empty;
        if (current.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await SwitchAsync();
        await Assertions.Expect(html).ToHaveAttributeAsync("lang", new System.Text.RegularExpressions.Regex($"^{lang}", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
