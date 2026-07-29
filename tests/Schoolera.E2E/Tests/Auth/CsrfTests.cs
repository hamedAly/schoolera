using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Auth;

[Collection(E2ECollection.Name)]
public sealed class CsrfTests : SchooleraPageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(SeedRole.Parent);
        return options;
    }

    [Fact]
    public async Task AuthenticatedPage_HasXsrfTokenCookie()
    {
        await GotoAsync("/parent/dashboard");
        await ExpectPageHealthyAsync();

        var cookies = await Page.Context.CookiesAsync();
        var xsrf = cookies.FirstOrDefault(c =>
            c.Name.Equals("XSRF-TOKEN", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(xsrf);
        Assert.False(string.IsNullOrWhiteSpace(xsrf.Value), "XSRF-TOKEN cookie should have a value.");
    }

    [Fact]
    public async Task PostWithoutCsrfToken_IsRejected()
    {
        await GotoAsync("/parent/dashboard");
        await ExpectPageHealthyAsync();

        var response = await Page.Context.APIRequest.PostAsync(
            $"{E2EConfig.BaseUrl}/api/parent/children",
            new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                },
                DataString = "{}",
            });

        Assert.True(
            response.Status is 400 or 401 or 403,
            $"Expected 400/401/403 for POST without CSRF token, got {response.Status}.");
    }
}
