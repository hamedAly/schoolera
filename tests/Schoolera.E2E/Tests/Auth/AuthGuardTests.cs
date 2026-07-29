using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;
using Schoolera.E2E.Pages.Auth;

namespace Schoolera.E2E.Tests.Auth;

public sealed class AuthGuardTests : SchooleraPageTest
{
    [Fact]
    public async Task Unauthenticated_ParentRoute_RedirectsToLogin()
    {
        await GotoAsync("/parent/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/auth/login.*"));
    }

    [Fact]
    public async Task Unauthenticated_AdminRoute_RedirectsToLogin()
    {
        await GotoAsync("/admin/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/auth/login.*"));
    }

    [Fact]
    public async Task Unauthenticated_SchoolRoute_RedirectsToLogin()
    {
        await GotoAsync("/school");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/auth/login.*"));
    }

    [Fact]
    public async Task Unauthenticated_SupportRoute_RedirectsToLogin()
    {
        await GotoAsync("/support/tickets");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/auth/login.*"));
    }
}

[Collection(E2ECollection.Name)]
public sealed class WrongRoleTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.Parent);
        return options;
    }

    [Fact]
    public async Task Parent_CannotAccess_AdminDashboard()
    {
        await GotoAsync("/admin/dashboard");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/(unauthorized|auth/login|parent).*"));
        var path = new Uri(Page.Url).AbsolutePath;
        Assert.False(path.StartsWith("/admin/dashboard", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class LoginSmokeTests : SchooleraPageTest
{
    [Fact]
    public async Task LoginPage_RendersEmailAndPassword()
    {
        var login = new LoginPage(Page);
        await login.GotoAsync();
        await Expect(login.Email).ToBeVisibleAsync();
        await Expect(login.Password).ToBeVisibleAsync();
        await Expect(login.Submit).ToBeVisibleAsync();
    }
}
