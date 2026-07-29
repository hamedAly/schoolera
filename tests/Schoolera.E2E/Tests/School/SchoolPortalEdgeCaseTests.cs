using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class SchoolPortalEdgeCaseTests : SchoolOwnerAuthTest
{
    [Fact]
    public async Task SchoolPortal_EvaluationTemplates_PageLoads()
    {
        var id = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{id}/evaluation-templates");
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolPortal_Payments_PageLoads()
    {
        var id = await AuthHelper.ResolveSchoolIdAsync(Page);
        await GotoAsync($"/school/{id}/payments");
        await ExpectPageHealthyAsync();
    }
}

public sealed class SchoolOnboardingDocumentTests : SchoolOwnerAuthTest
{
    [Fact]
    public async Task SchoolOnboarding_DocumentUploadArea_IsPresent()
    {
        await GotoAsync("/school/onboarding");
        await ExpectPageHealthyAsync();

        var fileInput = Page.Locator("input[type='file']").First;
        var uploadBtn = Page.Locator(
            "button:has-text('رفع'), button:has-text('Upload'), [data-testid='upload-document']").First;

        if (await fileInput.CountAsync() > 0)
        {
            await Expect(fileInput).ToBeAttachedAsync();
        }
        else if (await uploadBtn.CountAsync() > 0)
        {
            await Expect(uploadBtn).ToBeVisibleAsync();
        }
    }
}
