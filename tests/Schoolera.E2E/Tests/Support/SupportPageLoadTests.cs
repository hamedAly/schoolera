using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Support;

public sealed class SupportPageLoadTests : SupportAuthTest
{
    [Fact]
    public async Task Support_Tickets_List_Loads()
    {
        await GotoAsync("/support/tickets");
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }
}
