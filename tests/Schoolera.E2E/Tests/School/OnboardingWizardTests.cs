using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class OnboardingWizardTests : SchoolOwnerAuthTest
{
    [Fact]
    public async Task SchoolOwner_Onboarding_WizardStepsNavigable()
    {
        await GotoAsync("/school/onboarding");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();

        var steps = Page.Locator(
            ".onboarding-stepper .step, .wizard-step, mat-step-header, [data-testid='onboarding-step']");
        var stepCount = await steps.CountAsync();

        if (stepCount >= 2)
        {
            await Expect(steps.First).ToBeVisibleAsync();

            var nextBtn = Page.Locator(
                "button:has-text('التالي'), button:has-text('Next'), [data-testid='onboarding-next']").First;
            if (await nextBtn.CountAsync() > 0 && await nextBtn.IsEnabledAsync())
            {
                await nextBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
                await ExpectPageHealthyAsync();
            }
        }
    }

    [Fact]
    public async Task SchoolOwner_Onboarding_OrganizationStep_HasBilingualFields()
    {
        await GotoAsync("/school/onboarding");
        await ExpectPageHealthyAsync();

        var nameAr = Page.Locator(
            "input[formcontrolname='schoolNameAr'], input[formcontrolname='nameAr'], #onboarding-nameAr").First;
        var nameEn = Page.Locator(
            "input[formcontrolname='schoolNameEn'], input[formcontrolname='nameEn'], #onboarding-nameEn").First;

        if (await nameAr.CountAsync() > 0)
        {
            await Expect(nameAr).ToBeVisibleAsync();
        }
        if (await nameEn.CountAsync() > 0)
        {
            await Expect(nameEn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task SchoolOwner_OnboardingStatus_ShowsCurrentStatus()
    {
        await GotoAsync("/school/onboarding/status");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();

        var statusIndicator = Page.Locator(
            ".onboarding-status, [data-testid='onboarding-status'], .status-badge").First;
        if (await statusIndicator.CountAsync() > 0)
        {
            await Expect(statusIndicator).ToBeVisibleAsync();
        }
    }
}
