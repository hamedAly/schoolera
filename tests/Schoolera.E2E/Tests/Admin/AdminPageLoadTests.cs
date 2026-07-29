using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Admin;

public sealed class AdminPageLoadTests : AdminAuthTest
{
    public static TheoryData<string> AdminRoutes =>
    [
        "/admin/dashboard",
        "/admin/integrations",
        "/admin/integrations/new",
        "/admin/notification-templates",
        "/admin/notifications-ops",
        "/admin/inbound-messages",
        "/admin/onboarding",
        "/admin/applications",
        "/admin/schools",
        "/admin/users",
        "/admin/taxonomies",
        "/admin/cms/pages",
        "/admin/cms/pages/new",
        "/admin/cms/faq",
        "/admin/cms/home",
        "/admin/contact-requests",
        "/admin/support-tickets",
        "/admin/audit",
    ];

    [Theory]
    [MemberData(nameof(AdminRoutes))]
    public async Task AdminRoute_LoadsWithoutCrash(string path)
    {
        await GotoAsync(path);
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }
}
