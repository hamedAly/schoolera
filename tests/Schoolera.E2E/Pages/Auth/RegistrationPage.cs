using Microsoft.Playwright;

namespace Schoolera.E2E.Pages.Auth;

public sealed class RegistrationPage(IPage page)
{
    public ILocator FirstName => page.Locator("#firstName");
    public ILocator LastName => page.Locator("#lastName");
    public ILocator Email => page.Locator("#email");
    public ILocator PhoneNumber => page.Locator("#phoneNumber");
    public ILocator Password => page.Locator("#password");
    public ILocator ConfirmPassword => page.Locator("#confirmPassword");
    public ILocator TermsAccepted => page.Locator("#termsAccepted");
    public ILocator PrivacyAccepted => page.Locator("#privacyAccepted");
    public ILocator Submit => page.Locator("form.auth-form button[type='submit']");

    public async Task GotoParentAsync()
    {
        await page.GotoAsync("/auth/register/parent", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
    }

    public async Task GotoSchoolOwnerAsync()
    {
        await page.GotoAsync("/auth/register/school-owner", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
    }

    public async Task FillValidParentAsync(string suffix)
    {
        await FirstName.FillAsync($"E2EParentFirst{suffix}");
        await LastName.FillAsync($"E2EParentLast{suffix}");
        await Email.FillAsync($"e2e.{suffix}@schoolera.test");
        await PhoneNumber.FillAsync("+201000000099");
        await Password.FillAsync("E2eTest!234");
        await ConfirmPassword.FillAsync("E2eTest!234");

        if (await TermsAccepted.CountAsync() > 0)
            await TermsAccepted.CheckAsync();
        if (await PrivacyAccepted.CountAsync() > 0)
            await PrivacyAccepted.CheckAsync();
    }

    public async Task FillValidSchoolOwnerAsync(string suffix)
    {
        await FirstName.FillAsync($"E2ESchoolOwnerFirst{suffix}");
        await LastName.FillAsync($"E2ESchoolOwnerLast{suffix}");
        await Email.FillAsync($"e2e.{suffix}@schoolera.test");
        await PhoneNumber.FillAsync("+201000000099");
        await Password.FillAsync("E2eTest!234");
        await ConfirmPassword.FillAsync("E2eTest!234");

        if (await TermsAccepted.CountAsync() > 0)
            await TermsAccepted.CheckAsync();
    }
}
