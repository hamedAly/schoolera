using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;
using Schoolera.E2E.Pages.Public;

namespace Schoolera.E2E.Tests.Public;

public sealed class ContactMutationTests : SchooleraPageTest
{
    [Fact]
    public async Task Contact_Submit_RequiresConsentAndFields()
    {
        var contact = new ContactPage(Page);
        await contact.GotoAsync();
        await contact.Submit.ClickAsync();

        // Client validation should keep user on the form (no success banner).
        await Expect(contact.Success).ToHaveCountAsync(0);
        await Expect(contact.Heading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Contact_Submit_HappyPath()
    {
        var contact = new ContactPage(Page);
        await contact.GotoAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await contact.FillValidAsync(suffix);
        await contact.Submit.ClickAsync();
        await Expect(contact.Success).ToBeVisibleAsync();
    }
}
