using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Pages.Public;

public sealed class HomePage(IPage page)
{
    public ILocator Heading => page.Locator("h1").First;

    public async Task GotoAsync()
    {
        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }
}

public sealed class SchoolsListPage(IPage page)
{
    public ILocator Heading => page.Locator("h1").First;
    public ILocator SearchInput => page.Locator("#school-search-input");
    public ILocator ResultLinks => page.Locator("a[href*='/schools/']");

    public async Task GotoAsync()
    {
        await page.GotoAsync("/schools", new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await SearchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public async Task SearchAsync(string term)
    {
        await SearchInput.FillAsync(term);
        await page.WaitForTimeoutAsync(600);
    }
}

public sealed class SchoolProfilePage(IPage page)
{
    public ILocator Heading => page.Locator("h1").First;

    public async Task GotoAsync(string slug)
    {
        await page.GotoAsync($"/schools/{slug}", new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await page.Locator(".school-profile h1, #school-overview-heading").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = E2EConfig.NavigationTimeoutMs,
        });
    }
}

public sealed class ContactPage(IPage page)
{
    public ILocator Heading => page.Locator("h1").First;
    public ILocator Name => page.Locator("#contact-name");
    public ILocator Phone => page.Locator("#contact-phone");
    public ILocator Email => page.Locator("#contact-email");
    public ILocator Category => page.Locator("#contact-category");
    public ILocator Subject => page.Locator("#contact-subject");
    public ILocator Message => page.Locator("#contact-message");
    public ILocator Consent => page.Locator("#contact-consent");
    public ILocator Submit => page.Locator("form.contact-page__form button[type='submit']");
    public ILocator Success => page.Locator(".contact-page__success");

    public async Task GotoAsync()
    {
        await page.GotoAsync("/contact", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task FillValidAsync(string suffix)
    {
        await Name.FillAsync($"E2E User {suffix}");
        await Phone.FillAsync("+201000000001");
        await Email.FillAsync($"e2e.{suffix}@example.com");
        await Category.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Subject.FillAsync($"E2E subject {suffix}");
        await Message.FillAsync($"E2E automated contact message {suffix} with enough detail.");
        await Consent.CheckAsync();
    }
}
