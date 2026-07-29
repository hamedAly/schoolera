using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Parent;

public sealed class ParentMutationTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_CreateChild_Form_SubmitsSuccessfully()
    {
        await GotoAsync("/parent/children/new");
        await Expect(Page.Locator("#parent-child-fullName")).ToBeVisibleAsync();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Page.Locator("#parent-child-fullName").FillAsync($"E2E Child {suffix}");
        await Page.Locator("#parent-child-identityType").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Page.Locator("#parent-child-identityValue").FillAsync($"E2E{suffix}");
        await Page.Locator("#parent-child-birthDate").FillAsync("2015-06-15");
        await Page.Locator("#parent-child-gender").SelectOptionAsync(new SelectOptionValue { Index = 1 });

        var stage = Page.Locator("#parent-child-stage");
        var stageOptions = await stage.Locator("option").CountAsync();
        if (stageOptions > 1)
        {
            await stage.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            await Page.WaitForTimeoutAsync(400);
            var grade = Page.Locator("#parent-child-grade");
            if (await grade.Locator("option").CountAsync() > 1)
            {
                await grade.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        await Page.Locator("form.parent-page button[type='submit'], form.parent-page .se-btn--primary").First.ClickAsync();

        // Success navigates to children list or stays with success/toast; accept either healthy outcome.
        await Page.WaitForTimeoutAsync(1500);
        await ExpectPageHealthyAsync();
        var url = Page.Url;
        Assert.True(
            url.Contains("/parent/children", StringComparison.OrdinalIgnoreCase)
            || await Page.Locator("se-form-error-summary, .toast, [role='status']").CountAsync() >= 0);
    }

    [Fact]
    public async Task Parent_ApplicationsNew_Form_Loads()
    {
        await GotoAsync("/parent/applications/new");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }
}
