using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;
using Schoolera.E2E.Pages.Auth;

namespace Schoolera.E2E.Tests.Auth;

public sealed class LoginNegativeTests : SchooleraPageTest
{
    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var login = new LoginPage(Page);
        await login.LoginAsync(SeedUsers.Email(SeedRole.Parent), "WrongPassword!99");

        await Page.WaitForTimeoutAsync(1000);
        var errorVisible = await Page.Locator(
            "se-form-error-summary, .auth-form__error, .toast--error, [role='alert']").CountAsync();
        Assert.True(errorVisible > 0 || Page.Url.Contains("/auth/login"),
            "Expected error message or to remain on login page after wrong password.");
    }

    [Fact]
    public async Task Login_NonExistentEmail_ShowsError()
    {
        var login = new LoginPage(Page);
        await login.LoginAsync("nonexistent@schoolera.test", "AnyPassword!1");

        await Page.WaitForTimeoutAsync(1000);
        var errorVisible = await Page.Locator(
            "se-form-error-summary, .auth-form__error, .toast--error, [role='alert']").CountAsync();
        Assert.True(errorVisible > 0 || Page.Url.Contains("/auth/login"),
            "Expected error message or to remain on login page for non-existent email.");
    }

    [Fact]
    public async Task Login_EmptyFields_ShowsValidation()
    {
        var login = new LoginPage(Page);
        await login.GotoAsync();
        await login.Submit.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*/auth/login.*"));
        await ExpectPageHealthyAsync();
    }
}

[Collection(E2ECollection.Name)]
public sealed class LogoutTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.Parent);
        return options;
    }

    [Fact]
    public async Task Logout_RedirectsToPublicPage()
    {
        await GotoAsync("/parent/dashboard");
        await ExpectMainOrHeadingAsync();

        var logoutBtn = Page.Locator(
            "[data-testid='logout-button'], a[href*='logout'], button:has-text('خروج'), button:has-text('Logout')").First;
        if (await logoutBtn.CountAsync() > 0 && await logoutBtn.IsVisibleAsync())
        {
            await logoutBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            var path = new Uri(Page.Url).AbsolutePath;
            Assert.True(
                path == "/" || path.StartsWith("/auth/login", StringComparison.OrdinalIgnoreCase),
                $"Expected redirect to home or login after logout, got {path}.");
        }
    }
}

public sealed class RegistrationFormTests : SchooleraPageTest
{
    [Fact]
    public async Task ParentRegistration_Form_RendersAllFields()
    {
        var reg = new RegistrationPage(Page);
        await reg.GotoParentAsync();
        await ExpectPageHealthyAsync();
        await Expect(reg.FirstName).ToBeVisibleAsync();
        await Expect(reg.LastName).ToBeVisibleAsync();
        await Expect(reg.Email).ToBeVisibleAsync();
        await Expect(reg.PhoneNumber).ToBeVisibleAsync();
        await Expect(reg.Password).ToBeVisibleAsync();
        await Expect(reg.ConfirmPassword).ToBeVisibleAsync();
        await Expect(reg.TermsAccepted).ToBeVisibleAsync();
        await Expect(reg.PrivacyAccepted).ToBeVisibleAsync();
        await Expect(reg.Submit).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SchoolOwnerRegistration_Form_RendersAllFields()
    {
        var reg = new RegistrationPage(Page);
        await reg.GotoSchoolOwnerAsync();
        await ExpectPageHealthyAsync();
        await Expect(reg.FirstName).ToBeVisibleAsync();
        await Expect(reg.LastName).ToBeVisibleAsync();
        await Expect(reg.Email).ToBeVisibleAsync();
        await Expect(reg.PhoneNumber).ToBeVisibleAsync();
        await Expect(reg.Submit).ToBeVisibleAsync();
        await Expect(reg.Password).ToBeVisibleAsync();
        await Expect(reg.ConfirmPassword).ToBeVisibleAsync();
        await Expect(reg.TermsAccepted).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ParentRegistration_SubmitEmpty_ShowsValidation()
    {
        var reg = new RegistrationPage(Page);
        await reg.GotoParentAsync();
        await ExpectPageHealthyAsync();
        // On the registration page the submit button is disabled while the form is invalid.
        await Expect(reg.Submit).ToBeDisabledAsync();
    }

    [Fact]
    public async Task ParentRegistration_HappyPath_SubmitsSuccessfully()
    {
        var reg = new RegistrationPage(Page);
        await reg.GotoParentAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await reg.FillValidParentAsync(suffix);
        await reg.Submit.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await ExpectPageHealthyAsync();

        var url = Page.Url;
        Assert.True(
            url.Contains("/auth/verify") || url.Contains("/auth/login") || url.Contains("/parent"),
            $"Expected redirect to verify/login/parent after registration, got {url}.");
    }

    [Fact]
    public async Task SchoolOwnerRegistration_HappyPath_SubmitsSuccessfully()
    {
        var reg = new RegistrationPage(Page);
        await reg.GotoSchoolOwnerAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await reg.FillValidSchoolOwnerAsync(suffix);
        await reg.Submit.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);
        await ExpectPageHealthyAsync();

        var url = Page.Url;
        Assert.True(
            url.Contains("/auth/verify") || url.Contains("/auth/login") || url.Contains("/school"),
            $"Expected redirect to verify/login/school after registration, got {url}.");
    }
}

[Collection(E2ECollection.Name)]
public sealed class WrongRoleExtendedTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.SchoolOwner);
        return options;
    }

    [Fact]
    public async Task SchoolOwner_CannotAccess_AdminDashboard()
    {
        await GotoAsync("/admin/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/(unauthorized|auth/login|school).*"));
        var path = new Uri(Page.Url).AbsolutePath;
        Assert.False(path.StartsWith("/admin/dashboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SchoolOwner_CannotAccess_SupportTickets()
    {
        await GotoAsync("/support/tickets");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/(unauthorized|auth/login|school).*"));
        var path = new Uri(Page.Url).AbsolutePath;
        Assert.False(path.StartsWith("/support/tickets", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SchoolOwner_CannotAccess_ParentDashboard()
    {
        await GotoAsync("/parent/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/(unauthorized|auth/login|school).*"));
        var path = new Uri(Page.Url).AbsolutePath;
        Assert.False(path.StartsWith("/parent/dashboard", StringComparison.OrdinalIgnoreCase));
    }
}

[Collection(E2ECollection.Name)]
public sealed class SupportAgentWrongRoleTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.SupportAgent);
        return options;
    }

    [Fact]
    public async Task SupportAgent_CannotAccess_AdminDashboard()
    {
        await GotoAsync("/admin/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/(unauthorized|auth/login|support).*"));
        var path = new Uri(Page.Url).AbsolutePath;
        Assert.False(path.StartsWith("/admin/dashboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SupportAgent_CannotAccess_ParentDashboard()
    {
        await GotoAsync("/parent/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/(unauthorized|auth/login|support).*"));
        var path = new Uri(Page.Url).AbsolutePath;
        Assert.False(path.StartsWith("/parent/dashboard", StringComparison.OrdinalIgnoreCase));
    }
}
