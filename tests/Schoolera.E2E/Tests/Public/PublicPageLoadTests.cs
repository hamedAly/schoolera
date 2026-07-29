using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.Public;

public sealed class PublicPageLoadTests : SchooleraPageTest
{
    public static TheoryData<string> PublicRoutes =>
    [
        "/",
        "/schools",
        $"/schools/{E2EConfig.PublishedSchoolSlug}",
        "/about",
        "/faq",
        "/contact",
        "/how-it-works",
        "/privacy",
        "/terms",
        "/sla",
        "/auth/login",
        "/auth/account-type",
        "/auth/register/parent",
        "/auth/register/school-owner",
        "/auth/forgot-password",
        "/auth/verify",
        "/auth/reset-password",
    ];

    [Theory]
    [MemberData(nameof(PublicRoutes))]
    public async Task PublicRoute_LoadsWithoutCrash(string path)
    {
        await GotoAsync(path);
        await ExpectPageHealthyAsync();
        await ExpectMainOrHeadingAsync();
    }
}
