using Microsoft.Playwright;

namespace Schoolera.E2E.Pages.Auth;

public sealed class LoginPage(IPage page)
{
    public ILocator Email => page.Locator("#email");
    public ILocator Password => page.Locator("#password");
    public ILocator Submit => page.Locator("form.auth-form button[type='submit']");

    public async Task GotoAsync()
    {
        await page.GotoAsync("/auth/login", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
    }

    public async Task LoginAsync(string email, string password)
    {
        await GotoAsync();
        await Email.FillAsync(email);
        await Password.FillAsync(password);
        await Submit.ClickAsync();
    }
}
