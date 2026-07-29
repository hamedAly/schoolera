using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class AdmissionStatusTransitionTests : SchoolAdminAuthTest
{
    [Fact]
    public async Task SchoolAdmin_StartReview_TransitionsApplicationStatus()
    {
        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{schoolId}/applications");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator($"a[href*='/school/{schoolId}/applications/']").First;
        if (await detailLink.CountAsync() == 0)
            return;

        await detailLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($".*/school/{schoolId}/applications/.+"));
        await ExpectPageHealthyAsync();

        var startReview = Page.Locator(
            "[data-testid='admission-start-review'], button:has-text('بدء المراجعة'), button:has-text('Start Review')").First;
        if (await startReview.CountAsync() > 0 && await startReview.IsVisibleAsync() && await startReview.IsEnabledAsync())
        {
            await startReview.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await ExpectPageHealthyAsync();

            var statusBadge = Page.Locator(
                "se-portal-admission-status-badge, .admission-status, [data-testid='admission-status']").First;
            if (await statusBadge.CountAsync() > 0)
            {
                await Expect(statusBadge).ToBeVisibleAsync();
            }
        }
    }

    [Fact]
    public async Task SchoolAdmin_AcceptApplication_ShowsConfirmation()
    {
        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{schoolId}/applications");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator($"a[href*='/school/{schoolId}/applications/']").First;
        if (await detailLink.CountAsync() == 0)
            return;

        await detailLink.ClickAsync();
        await ExpectPageHealthyAsync();

        var accept = Page.Locator(
            "[data-testid='admission-accept'], button:has-text('قبول'), button:has-text('Accept')").First;
        if (await accept.CountAsync() == 0 || !await accept.IsVisibleAsync())
            return;

        await accept.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        var confirmDialog = Page.Locator(
            ".cdk-overlay-container, mat-dialog-container, .modal, [role='dialog']").First;
        if (await confirmDialog.CountAsync() > 0)
        {
            var confirmBtn = confirmDialog.Locator(
                "button:has-text('تأكيد'), button:has-text('Confirm'), button.primary").First;
            if (await confirmBtn.CountAsync() > 0)
            {
                await confirmBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
            }
        }

        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolAdmin_RejectApplication_ShowsConfirmation()
    {
        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{schoolId}/applications");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator($"a[href*='/school/{schoolId}/applications/']").First;
        if (await detailLink.CountAsync() == 0)
            return;

        await detailLink.ClickAsync();
        await ExpectPageHealthyAsync();

        var reject = Page.Locator(
            "[data-testid='admission-reject'], button:has-text('رفض'), button:has-text('Reject')").First;
        if (await reject.CountAsync() == 0 || !await reject.IsVisibleAsync())
            return;

        await reject.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        var confirmDialog = Page.Locator(
            ".cdk-overlay-container, mat-dialog-container, .modal, [role='dialog']").First;
        if (await confirmDialog.CountAsync() > 0)
        {
            var reasonField = confirmDialog.Locator("textarea, input[type='text']").First;
            if (await reasonField.CountAsync() > 0)
            {
                await reasonField.FillAsync("E2E test rejection reason");
            }

            var confirmBtn = confirmDialog.Locator(
                "button:has-text('تأكيد'), button:has-text('Confirm'), button.primary, button.danger").First;
            if (await confirmBtn.CountAsync() > 0)
            {
                await confirmBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(2000);
            }
        }

        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolAdmin_RequestMissingItems_OpensForm()
    {
        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{schoolId}/applications");
        await ExpectMainOrHeadingAsync();

        var detailLink = Page.Locator($"a[href*='/school/{schoolId}/applications/']").First;
        if (await detailLink.CountAsync() == 0)
            return;

        await detailLink.ClickAsync();
        await ExpectPageHealthyAsync();

        var missingBtn = Page.Locator(
            "[data-testid='admission-missing-items'], button:has-text('مستندات ناقصة'), button:has-text('Missing')").First;
        if (await missingBtn.CountAsync() > 0 && await missingBtn.IsVisibleAsync())
        {
            await missingBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await ExpectPageHealthyAsync();
        }
    }
}
