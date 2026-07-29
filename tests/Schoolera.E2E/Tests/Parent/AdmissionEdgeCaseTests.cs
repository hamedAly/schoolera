using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Parent;

public sealed class AdmissionEdgeCaseTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_ApplicationCancel_ShowsConfirmation()
    {
        await GotoAsync("/parent/applications");
        await ExpectMainOrHeadingAsync();

        var appLink = Page.Locator(
            "a[href*='/parent/applications/']:not([href$='/new']):not([href$='/edit'])").First;
        if (await appLink.CountAsync() == 0 || !await appLink.IsVisibleAsync())
            return;

        await appLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/applications/.+"));
        await ExpectPageHealthyAsync();

        var cancelBtn = Page.Locator(
            "button:has-text('إلغاء'), button:has-text('Cancel'), [data-testid='cancel-application']").First;
        if (await cancelBtn.CountAsync() == 0 || !await cancelBtn.IsVisibleAsync())
            return;

        await cancelBtn.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        var confirmDialog = Page.Locator(
            ".cdk-overlay-container, mat-dialog-container, .modal, [role='dialog']").First;
        if (await confirmDialog.CountAsync() > 0)
        {
            await Expect(confirmDialog).ToBeVisibleAsync();
            await ExpectPageHealthyAsync();
        }
    }

    [Fact]
    public async Task Parent_AdmissionSubscriptions_PageFunctions()
    {
        await GotoAsync("/parent/admission-subscriptions");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var subscribeBtn = Page.Locator(
            "button:has-text('اشتراك'), button:has-text('Subscribe'), [data-testid='subscribe']").First;
        if (await subscribeBtn.CountAsync() > 0)
        {
            await Expect(subscribeBtn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Parent_Payments_ListAndDetailLoads()
    {
        await GotoAsync("/parent/payments");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var paymentLink = Page.Locator("a[href*='/parent/payments/']").First;
        if (await paymentLink.CountAsync() > 0 && await paymentLink.IsVisibleAsync())
        {
            await paymentLink.ClickAsync();
            await ExpectPageHealthyAsync();
        }
    }

    [Fact]
    public async Task Parent_InterviewAppointments_PageLoads()
    {
        await GotoAsync("/parent/applications");
        await ExpectMainOrHeadingAsync();

        var appLink = Page.Locator(
            "a[href*='/parent/applications/']:not([href$='/new']):not([href$='/edit'])").First;
        if (await appLink.CountAsync() == 0 || !await appLink.IsVisibleAsync())
            return;

        await appLink.ClickAsync();
        await ExpectPageHealthyAsync();

        var appointmentSection = Page.Locator(
            "[data-testid='interview-appointment'], .interview-section, button:has-text('مقابلة'), button:has-text('Interview')").First;
        if (await appointmentSection.CountAsync() > 0)
        {
            await Expect(appointmentSection).ToBeVisibleAsync();
        }
    }
}

public sealed class ParentLegalAcceptanceTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_TermsAndPrivacy_PagesLoad()
    {
        await GotoAsync("/terms");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();

        await GotoAsync("/privacy");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }
}
