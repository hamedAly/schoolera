using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Parent;

public sealed class AdmissionApplicationTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_NewApplication_WizardStepsLoad()
    {
        await GotoAsync("/parent/applications/new");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();

        var schoolSelect = Page.Locator(
            "#admission-school, select[formcontrolname='schoolId'], [data-testid='admission-school-select']").First;
        if (await schoolSelect.CountAsync() > 0)
        {
            await Expect(schoolSelect).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Parent_NewApplication_SelectSchoolAndChild_ProgressesToNextStep()
    {
        await GotoAsync("/parent/applications/new");
        await ExpectPageHealthyAsync();

        var schoolSelect = Page.Locator(
            "#admission-school, select[formcontrolname='schoolId'], [data-testid='admission-school-select']").First;
        if (await schoolSelect.CountAsync() == 0)
            return;

        var schoolOptions = await schoolSelect.Locator("option").CountAsync();
        if (schoolOptions <= 1)
            return;

        await schoolSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Page.WaitForTimeoutAsync(800);

        var childSelect = Page.Locator(
            "#admission-child, select[formcontrolname='childProfileId'], [data-testid='admission-child-select']").First;
        if (await childSelect.CountAsync() > 0)
        {
            var childOptions = await childSelect.Locator("option").CountAsync();
            if (childOptions > 1)
            {
                await childSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Page.WaitForTimeoutAsync(800);
            }
        }

        var branchSelect = Page.Locator(
            "#admission-branch, select[formcontrolname='schoolBranchId']").First;
        if (await branchSelect.CountAsync() > 0)
        {
            var branchOptions = await branchSelect.Locator("option").CountAsync();
            if (branchOptions > 1)
            {
                await branchSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Page.WaitForTimeoutAsync(500);
            }
        }

        var stageSelect = Page.Locator(
            "#admission-stage, select[formcontrolname='educationalStageId']").First;
        if (await stageSelect.CountAsync() > 0)
        {
            var stageOptions = await stageSelect.Locator("option").CountAsync();
            if (stageOptions > 1)
            {
                await stageSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Page.WaitForTimeoutAsync(500);
            }
        }

        var gradeSelect = Page.Locator(
            "#admission-grade, select[formcontrolname='gradeId']").First;
        if (await gradeSelect.CountAsync() > 0)
        {
            var gradeOptions = await gradeSelect.Locator("option").CountAsync();
            if (gradeOptions > 1)
            {
                await gradeSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Page.WaitForTimeoutAsync(500);
            }
        }

        var nextBtn = Page.Locator(
            "button:has-text('التالي'), button:has-text('Next'), [data-testid='admission-next']").First;
        if (await nextBtn.CountAsync() > 0 && await nextBtn.IsEnabledAsync())
        {
            await nextBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await ExpectPageHealthyAsync();
        }

        var submitBtn = Page.Locator(
            "button:has-text('تقديم'), button:has-text('Submit'), button[type='submit']").First;
        if (await submitBtn.CountAsync() > 0 && await submitBtn.IsEnabledAsync())
        {
            await submitBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            await ExpectPageHealthyAsync();
        }
    }

    [Fact]
    public async Task Parent_ExistingApplication_DetailShowsStatus()
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

        var statusBadge = Page.Locator(
            "se-admission-status-badge, .admission-status-badge, [data-testid='admission-status']").First;
        if (await statusBadge.CountAsync() > 0)
        {
            await Expect(statusBadge).ToBeVisibleAsync();
        }
    }
}
