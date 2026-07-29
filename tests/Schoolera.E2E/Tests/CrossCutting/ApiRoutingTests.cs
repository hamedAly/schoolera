using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Schoolera.E2E.Tests.CrossCutting;

public sealed class ApiRoutingTests : SchooleraPageTest
{
    [Fact]
    public async Task Api_NonExistentEndpoint_ReturnsJsonNotHtml()
    {
        await GotoAsync("/");

        var response = await Page.Context.APIRequest.GetAsync(
            $"{E2EConfig.BaseUrl}/api/nonexistent-endpoint-e2e-test");

        Assert.Equal(404, response.Status);
        var body = await response.TextAsync();
        if (!string.IsNullOrWhiteSpace(body))
        {
            // Ensure it's valid JSON when the server returns a body.
            _ = JsonDocument.Parse(body);
        }
        Assert.DoesNotContain("<!DOCTYPE", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Swagger_NonExistent_ReturnsNotFoundNotHtml()
    {
        await GotoAsync("/");

        var response = await Page.Context.APIRequest.GetAsync(
            $"{E2EConfig.BaseUrl}/swagger/nonexistent");

        Assert.True(response.Status is 404 or 301 or 302,
            $"Expected 404/redirect for /swagger/nonexistent, got {response.Status}.");
        var body = await response.TextAsync();
        Assert.DoesNotContain("<app-root", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Api_Unauthenticated_ReturnsJsonNotHtml()
    {
        await GotoAsync("/");

        var response = await Page.Context.APIRequest.GetAsync(
            $"{E2EConfig.BaseUrl}/api/parent/profile");

        Assert.True(response.Status is 401 or 403,
            $"Expected 401/403 for unauthenticated /api/parent/profile, got {response.Status}.");
        var body = await response.TextAsync();
        _ = JsonDocument.Parse(body);
    }

    [Fact]
    public async Task Frontend_InvalidRoute_ShowsAngularNotFoundPage()
    {
        await GotoAsync("/this-route-does-not-exist-e2e");
        await ExpectPageHealthyAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("this-route-does-not-exist-e2e", RegexOptions.IgnoreCase));
        Assert.DoesNotContain("/auth/login", Page.Url, StringComparison.OrdinalIgnoreCase);
    }
}
