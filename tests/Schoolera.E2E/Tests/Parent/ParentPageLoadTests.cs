using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Parent;

public sealed class ParentPageLoadTests : ParentAuthTest
{
    public static TheoryData<string> ParentRoutes =>
    [
        "/parent/dashboard",
        "/parent/profile",
        "/parent/children",
        "/parent/children/new",
        "/parent/applications",
        "/parent/applications/new",
        "/parent/notifications",
        "/parent/notification-preferences",
        "/parent/admission-subscriptions",
        "/parent/favorites",
        "/parent/payments",
        "/parent/support-tickets",
        "/parent/support-tickets/new",
    ];

    [Theory]
    [MemberData(nameof(ParentRoutes))]
    public async Task ParentRoute_LoadsWithoutCrash(string path)
    {
        await GotoAsync(path);
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }
}
