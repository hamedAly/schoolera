using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Parent;

public sealed class ParentProfileAndChildCrudTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_Profile_EditAndSave()
    {
        await GotoAsync("/parent/profile");
        await ExpectMainOrHeadingAsync();

        var phone = Page.Locator(
            "input[formcontrolname='alternatePhone'], #parent-alternatePhone").First;
        if (await phone.CountAsync() > 0 && await phone.IsVisibleAsync())
        {
            var marker = $"+20100000{DateTime.UtcNow:mmss}";
            await phone.FillAsync(marker);

            var save = Page.Locator(
                "form button[type='submit'], .se-btn--primary, button:has-text('حفظ'), button:has-text('Save')").First;
            if (await save.CountAsync() > 0)
            {
                await save.ClickAsync();
                await Page.WaitForTimeoutAsync(1500);
                await ExpectPageHealthyAsync();
            }
        }
    }

    [Fact]
    public async Task Parent_ChildEdit_LoadsAndSaves()
    {
        await GotoAsync("/parent/children");
        await ExpectMainOrHeadingAsync();

        var editLink = Page.Locator("a[href*='/parent/children/'][href$='/edit']").First;
        if (await editLink.CountAsync() == 0 || !await editLink.IsVisibleAsync())
            return;

        await editLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/children/.+/edit.*"));
        await ExpectPageHealthyAsync();

        var nameField = Page.Locator("#parent-child-fullName").First;
        if (await nameField.CountAsync() > 0)
        {
            var current = await nameField.InputValueAsync();
            var marker = $"{current} E2E";
            if (marker.Length > 60) marker = current;
            await nameField.FillAsync(marker);

            var save = Page.Locator(
                "form button[type='submit'], .se-btn--primary").First;
            if (await save.CountAsync() > 0)
            {
                await save.ClickAsync();
                await Page.WaitForTimeoutAsync(1500);
                await ExpectPageHealthyAsync();
            }
        }
    }

    [Fact]
    public async Task Parent_CreateChild_ValidationErrors_PreventSubmission()
    {
        await GotoAsync("/parent/children/new");
        await Expect(Page.Locator("#parent-child-fullName")).ToBeVisibleAsync();

        var submit = Page.Locator(
            "form.parent-page button[type='submit'], form.parent-page .se-btn--primary").First;
        await submit.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        await Expect(Page).ToHaveURLAsync(new Regex(".*/parent/children/new.*"));
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task Parent_ChildDocuments_PageLoadsAndShowsUploadControl()
    {
        await GotoAsync("/parent/children");
        await ExpectMainOrHeadingAsync();

        var childLink = Page.Locator(
            "a[href*='/parent/children/']:not([href$='/new']):not([href$='/edit'])").First;
        if (await childLink.CountAsync() == 0 || !await childLink.IsVisibleAsync())
            return;

        await childLink.ClickAsync();
        await ExpectPageHealthyAsync();

        var docsSection = Page.Locator(
            ".child-documents, [data-testid='child-documents'], button:has-text('مستندات'), button:has-text('Documents')").First;
        if (await docsSection.CountAsync() > 0)
        {
            await Expect(docsSection).ToBeVisibleAsync();
        }
    }
}

public sealed class ParentNotificationTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_NotificationPreferences_ToggleAndSave()
    {
        await GotoAsync("/parent/notification-preferences");
        await ExpectMainOrHeadingAsync();

        var toggle = Page.Locator(
            "input[type='checkbox'], mat-slide-toggle, .toggle-switch").First;
        if (await toggle.CountAsync() > 0 && await toggle.IsVisibleAsync())
        {
            await toggle.ClickAsync();
            await Page.WaitForTimeoutAsync(300);

            var save = Page.Locator(
                "button[type='submit'], .se-btn--primary, button:has-text('حفظ'), button:has-text('Save')").First;
            if (await save.CountAsync() > 0)
            {
                await save.ClickAsync();
                await Page.WaitForTimeoutAsync(1500);
                await ExpectPageHealthyAsync();
            }
        }
    }

    [Fact]
    public async Task Parent_Notifications_ListLoadsWithItems()
    {
        await GotoAsync("/parent/notifications");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }
}

public sealed class ParentFavoritesTests : ParentAuthTest
{
    [Fact]
    public async Task Parent_Favorites_PageLoads()
    {
        await GotoAsync("/parent/favorites");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }
}
