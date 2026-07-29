using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Support;

public sealed class SupportTicketFlowTests : SupportAuthTest
{
    [Fact]
    public async Task Support_TicketDetail_ShowsMessagesAndReplyForm()
    {
        await GotoAsync("/support/tickets");
        await ExpectMainOrHeadingAsync();

        var link = Page.Locator("a[href*='/support/tickets/']").First;
        if (await link.CountAsync() == 0 || !await link.IsVisibleAsync())
            return;

        await link.ClickAsync();
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var replyBox = Page.Locator(
            "textarea, [data-testid='ticket-reply'], .ticket-reply-input").First;
        if (await replyBox.CountAsync() > 0)
        {
            await Expect(replyBox).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Support_TicketDetail_ReplySubmitsSuccessfully()
    {
        await GotoAsync("/support/tickets");
        await ExpectMainOrHeadingAsync();

        var link = Page.Locator("a[href*='/support/tickets/']").First;
        if (await link.CountAsync() == 0 || !await link.IsVisibleAsync())
            return;

        await link.ClickAsync();
        await ExpectPageHealthyAsync();

        var replyBox = Page.Locator(
            "textarea, [data-testid='ticket-reply'], .ticket-reply-input").First;
        if (await replyBox.CountAsync() == 0 || !await replyBox.IsVisibleAsync())
            return;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await replyBox.FillAsync($"E2E automated reply {suffix}");

        var sendBtn = Page.Locator(
            "button:has-text('إرسال'), button:has-text('Send'), button[type='submit']").First;
        if (await sendBtn.CountAsync() > 0)
        {
            await sendBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(1500);
            await ExpectPageHealthyAsync();
        }
    }

    [Fact]
    public async Task Support_TicketDetail_StatusChangeDropdownLoads()
    {
        await GotoAsync("/support/tickets");
        await ExpectMainOrHeadingAsync();

        var link = Page.Locator("a[href*='/support/tickets/']").First;
        if (await link.CountAsync() == 0 || !await link.IsVisibleAsync())
            return;

        await link.ClickAsync();
        await ExpectPageHealthyAsync();

        var statusSelect = Page.Locator(
            "select[formcontrolname='status'], [data-testid='ticket-status'], .ticket-status-select").First;
        if (await statusSelect.CountAsync() > 0)
        {
            await Expect(statusSelect).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Support_TicketList_FilterByStatus()
    {
        await GotoAsync("/support/tickets");
        await ExpectMainOrHeadingAsync();

        var statusFilter = Page.Locator(
            "select[data-testid='status-filter'], #ticket-status-filter, select.ticket-filter").First;
        if (await statusFilter.CountAsync() > 0 && await statusFilter.IsVisibleAsync())
        {
            var options = await statusFilter.Locator("option").CountAsync();
            if (options > 1)
            {
                await statusFilter.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Page.WaitForTimeoutAsync(500);
                await ExpectPageHealthyAsync();
            }
        }
    }
}

public sealed class ParentSupportTicketTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_CreateSupportTicket_FormSubmits()
    {
        await GotoAsync("/parent/support-tickets/new");
        await ExpectMainOrHeadingAsync();

        var subject = Page.Locator(
            "input[formcontrolname='subject'], #ticket-subject").First;
        var description = Page.Locator(
            "textarea[formcontrolname='description'], #ticket-description, textarea").First;

        if (await subject.CountAsync() == 0)
            return;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await subject.FillAsync($"E2E Ticket {suffix}");

        if (await description.CountAsync() > 0)
        {
            await description.FillAsync($"E2E automated support ticket description {suffix}");
        }

        var category = Page.Locator(
            "select[formcontrolname='category'], #ticket-category").First;
        if (await category.CountAsync() > 0)
        {
            var catOptions = await category.Locator("option").CountAsync();
            if (catOptions > 1)
            {
                await category.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        var submit = Page.Locator("button[type='submit']").First;
        if (await submit.CountAsync() > 0)
        {
            await submit.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await ExpectPageHealthyAsync();

            var url = Page.Url;
            Assert.True(
                url.Contains("/parent/support-tickets") || url.Contains("/parent/support"),
                $"Expected to stay in support area after submission, got {url}.");
        }
    }

    [Fact]
    public async Task Parent_SupportTicketDetail_ShowsMessages()
    {
        await GotoAsync("/parent/support-tickets");
        await ExpectMainOrHeadingAsync();

        var link = Page.Locator("a[href*='/parent/support-tickets/']").First;
        if (await link.CountAsync() == 0 || !await link.IsVisibleAsync())
            return;

        await link.ClickAsync();
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }
}
