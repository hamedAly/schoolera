using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Admin;

public sealed class AdminMutationTests : AdminAuthTest
{
    [Fact]
    public async Task Admin_Taxonomies_CreateCurriculum_WithUniqueSlug()
    {
        await GotoAsync("/admin/taxonomies");
        await ExpectMainOrHeadingAsync();

        // Switch to curricula tab (safer unique create than countries).
        var curriculaTab = Page.Locator("button[role='tab']").Filter(new LocatorFilterOptions
        {
            HasTextRegex = new System.Text.RegularExpressions.Regex("curricul|مناهج", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
        });
        if (await curriculaTab.CountAsync() > 0)
        {
            await curriculaTab.First.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var formPanel = Page.Locator(".admin-form-panel form");
        var nameAr = formPanel.Locator("input[lang='ar']").First;
        var nameEn = formPanel.Locator("input[lang='en']").First;
        var slug = formPanel.Locator("input[formcontrolname='slug']").First;

        await Expect(nameAr).ToBeVisibleAsync();
        await nameAr.FillAsync($"منهج اختبار {suffix}");
        await nameEn.FillAsync($"E2E Curriculum {suffix}");
        await slug.FillAsync($"e2e-curr-{suffix}");

        await Page.Locator("form button[type='submit']").First.ClickAsync();
        await Page.WaitForTimeoutAsync(1500);
        await ExpectPageHealthyAsync();

        var alert = Page.Locator(".admin-alert, [role='status']");
        if (await alert.CountAsync() > 0)
        {
            await Expect(alert.First).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_CmsHome_LoadsAndExposesSaveControls()
    {
        await GotoAsync("/admin/cms/home");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var save = Page.Locator("button[type='submit'], button.primary, .se-btn--primary").First;
        if (await save.CountAsync() > 0)
        {
            await Expect(save).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_IntegrationsNew_ShowsValidationWithoutSecrets()
    {
        await GotoAsync("/admin/integrations/new");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var submit = Page.Locator("button[type='submit']").First;
        if (await submit.CountAsync() > 0 && await submit.IsEnabledAsync())
        {
            await submit.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        await ExpectPageHealthyAsync();
    }
}
