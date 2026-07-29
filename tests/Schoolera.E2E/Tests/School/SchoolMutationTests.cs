using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class SchoolMutationTests : SchoolOwnerAuthTest
{
    [Fact]
    public async Task SchoolOwner_Profile_CanEditShortDescriptionAndSave()
    {
        var schoolId = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{schoolId}/profile");
        await ExpectMainOrHeadingAsync();

        // Bilingual short description English control — prefer name attribute / form control.
        var shortEn = Page.Locator(
            "textarea[formcontrolname='shortDescriptionEn'], input[formcontrolname='shortDescriptionEn'], textarea[formControlName='shortDescriptionEn']").First;
        if (await shortEn.CountAsync() == 0)
        {
            // Fallback: second textarea in bilingual groups.
            shortEn = Page.Locator("form.portal-profile textarea").Nth(1);
        }

        await Expect(shortEn).ToBeVisibleAsync();
        var marker = $"E2E {DateTime.UtcNow:HHmmss}";
        await shortEn.FillAsync(marker);

        var save = Page.Locator("se-portal-page-header button, form.portal-profile button[type='submit']").First;
        await save.ClickAsync();

        await Page.WaitForTimeoutAsync(1500);
        await ExpectPageHealthyAsync();
        var success = Page.Locator(".portal-profile__success, [role='status']");
        if (await success.CountAsync() > 0)
        {
            await Expect(success.First).ToBeVisibleAsync();
        }
    }
}
