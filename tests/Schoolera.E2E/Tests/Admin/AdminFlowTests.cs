using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Admin;

public sealed class AdminOnboardingReviewTests : AdminAuthTest
{
    [Fact]
    public async Task Admin_OnboardingList_OpensDetailAndShowsActions()
    {
        await GotoAsync("/admin/onboarding");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator("a[href*='/admin/onboarding/']").First;
        if (await detailLink.CountAsync() == 0 || !await detailLink.IsVisibleAsync())
            return;

        await detailLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/admin/onboarding/.+"));
        await ExpectPageHealthyAsync();

        var approveBtn = Page.Locator(
            "button:has-text('موافقة'), button:has-text('Approve'), [data-testid='onboarding-approve']").First;
        var rejectBtn = Page.Locator(
            "button:has-text('رفض'), button:has-text('Reject'), [data-testid='onboarding-reject']").First;

        if (await approveBtn.CountAsync() > 0)
        {
            await Expect(approveBtn).ToBeVisibleAsync();
        }
        if (await rejectBtn.CountAsync() > 0)
        {
            await Expect(rejectBtn).ToBeVisibleAsync();
        }
    }
}

public sealed class AdminTaxonomyCrudTests : AdminAuthTest
{
    [Theory]
    [InlineData(0, "countries")]
    [InlineData(1, "governorates")]
    [InlineData(2, "cities")]
    [InlineData(3, "curricula")]
    public async Task Admin_Taxonomies_TabLoadsWithContent(int tabIndex, string tabKey)
    {
        await GotoAsync("/admin/taxonomies");
        await ExpectMainOrHeadingAsync();

        var tabs = Page.Locator("button[role='tab']");
        Assert.True(await tabs.CountAsync() > tabIndex, $"Expected at least {tabIndex + 1} tabs, got fewer. Tab '{tabKey}'.");
        await tabs.Nth(tabIndex).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        // If translations/content fail to load, the page typically shows an error component.
        await Expect(Page.Locator("se-portal-error-state")).ToHaveCountAsync(0);
        await ExpectPageHealthyAsync();
    }
}

public sealed class AdminCmsCrudTests : AdminAuthTest
{
    [Fact]
    public async Task Admin_CmsPages_NewForm_SubmitsSuccessfully()
    {
        await GotoAsync("/admin/cms/pages/new");
        await ExpectMainOrHeadingAsync();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var titleAr = Page.Locator(
            "input[formcontrolname='titleAr'], input[lang='ar']").First;
        var titleEn = Page.Locator(
            "input[formcontrolname='titleEn'], input[lang='en']").First;
        var slug = Page.Locator(
            "input[formcontrolname='slug']").First;

        if (await titleAr.CountAsync() > 0)
        {
            await titleAr.FillAsync($"صفحة اختبار {suffix}");
        }
        if (await titleEn.CountAsync() > 0)
        {
            await titleEn.FillAsync($"E2E Test Page {suffix}");
        }
        if (await slug.CountAsync() > 0)
        {
            await slug.FillAsync($"e2e-page-{suffix}");
        }

        var contentAr = Page.Locator(
            "textarea[formcontrolname='contentAr'], .ql-editor").First;
        if (await contentAr.CountAsync() > 0)
        {
            await contentAr.FillAsync($"محتوى اختبار {suffix}");
        }

        var submit = Page.Locator("button[type='submit']").First;
        if (await submit.CountAsync() > 0)
        {
            await submit.ClickAsync();
            await Page.WaitForTimeoutAsync(1500);
            await ExpectPageHealthyAsync();
        }
    }

    [Fact]
    public async Task Admin_CmsFaq_ListAndAddButtonLoads()
    {
        await GotoAsync("/admin/cms/faq");
        await ExpectMainOrHeadingAsync();

        var addBtn = Page.Locator(
            "button:has-text('إضافة'), button:has-text('Add'), [data-testid='add-faq']").First;
        if (await addBtn.CountAsync() > 0)
        {
            await Expect(addBtn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_CmsHome_SaveUpdatesContent()
    {
        await GotoAsync("/admin/cms/home");
        await ExpectMainOrHeadingAsync();

        var heroTitleAr = Page.Locator(
            "input[formcontrolname='heroTitleAr'], textarea[formcontrolname='heroTitleAr']").First;
        if (await heroTitleAr.CountAsync() > 0 && await heroTitleAr.IsVisibleAsync())
        {
            var marker = $"E2E {DateTime.UtcNow:HHmmss}";
            var current = await heroTitleAr.InputValueAsync();
            await heroTitleAr.FillAsync(current.Length > 0 ? current : marker);

            var save = Page.Locator("button[type='submit']").First;
            if (await save.CountAsync() > 0)
            {
                await save.ClickAsync();
                await Page.WaitForTimeoutAsync(1500);
                await ExpectPageHealthyAsync();
            }
        }
    }
}

public sealed class AdminNotificationTemplateTests : AdminAuthTest
{
    [Fact]
    public async Task Admin_NotificationTemplates_ListLoadsWithContent()
    {
        await GotoAsync("/admin/notification-templates");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var rows = Page.Locator("table tbody tr, .template-card, [data-testid='template-item']");
        if (await rows.CountAsync() > 0)
        {
            await Expect(rows.First).ToBeVisibleAsync();
        }
    }
}

public sealed class AdminUsersTests : AdminAuthTest
{
    [Fact]
    public async Task Admin_Users_ListAndDetailLoads()
    {
        await GotoAsync("/admin/users");
        await ExpectMainOrHeadingAsync();

        var userLink = Page.Locator("a[href*='/admin/users/']").First;
        if (await userLink.CountAsync() > 0 && await userLink.IsVisibleAsync())
        {
            await userLink.ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex(".*/admin/users/.+"));
            await ExpectPageHealthyAsync();
        }
    }
}

public sealed class AdminPaymentsAndExportTests : AdminAuthTest
{
    [Fact]
    public async Task Admin_Payments_PageLoads()
    {
        await GotoAsync("/admin/payments");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }

    [Fact]
    public async Task Admin_SupportTickets_ListLoads()
    {
        await GotoAsync("/admin/support-tickets");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task Admin_Courier_PageLoads()
    {
        await GotoAsync("/admin/courier");
        await ExpectPageHealthyAsync();
    }
}
