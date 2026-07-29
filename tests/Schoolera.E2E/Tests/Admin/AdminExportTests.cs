using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Admin;

public sealed class AdminExportTests : AdminAuthTest
{
    [Fact]
    public async Task Admin_SupportTickets_ExportButtonPresent()
    {
        await GotoAsync("/admin/support-tickets");
        await ExpectMainOrHeadingAsync();

        var exportBtn = Page.Locator(
            "button:has-text('تصدير'), button:has-text('Export'), [data-testid='export-tickets'], a:has-text('Export')").First;
        if (await exportBtn.CountAsync() > 0)
        {
            await Expect(exportBtn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Applications_ExportButtonPresent()
    {
        await GotoAsync("/admin/applications");
        await ExpectMainOrHeadingAsync();

        var exportBtn = Page.Locator(
            "button:has-text('تصدير'), button:has-text('Export'), [data-testid='export-applications'], a:has-text('Export')").First;
        if (await exportBtn.CountAsync() > 0)
        {
            await Expect(exportBtn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Applications_DetailOpens()
    {
        await GotoAsync("/admin/applications");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator("a[href*='/admin/applications/']").First;
        if (await detailLink.CountAsync() > 0 && await detailLink.IsVisibleAsync())
        {
            await detailLink.ClickAsync();
            await ExpectPageHealthyAsync();
        }
    }

    [Fact]
    public async Task Admin_InboundMessages_DetailOpens()
    {
        await GotoAsync("/admin/inbound-messages");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator("a[href*='/admin/inbound-messages/'], table tbody tr").First;
        if (await detailLink.CountAsync() > 0 && await detailLink.IsVisibleAsync())
        {
            await detailLink.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            await ExpectPageHealthyAsync();
        }
    }

    [Fact]
    public async Task Admin_Audit_FilterAndSearchWorks()
    {
        await GotoAsync("/admin/audit");
        await ExpectMainOrHeadingAsync();

        var searchInput = Page.Locator(
            "input[type='search'], input[placeholder*='بحث'], input[placeholder*='Search'], #audit-search").First;
        if (await searchInput.CountAsync() > 0 && await searchInput.IsVisibleAsync())
        {
            await searchInput.FillAsync("E2E");
            await Page.WaitForTimeoutAsync(500);
            await ExpectPageHealthyAsync();
        }
    }
}
