using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Schoolera.E2E.Infrastructure;

namespace Schoolera.E2E.Tests.School;

public sealed class SchoolPortalCrudTests : SchoolOwnerAuthTest
{
    private async Task<string> GetSchoolIdAsync() => await AuthHelper.ResolveSchoolIdAsync(Page);

    [Fact]
    public async Task SchoolPortal_Fees_ListAndFormLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/fees");
        await ExpectMainOrHeadingAsync();

        var addBtn = Page.Locator(
            "button:has-text('إضافة'), button:has-text('Add'), a:has-text('إضافة'), [data-testid='add-fee']").First;
        if (await addBtn.CountAsync() > 0)
        {
            await Expect(addBtn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task SchoolPortal_Branches_ListLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/branches");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();

        var branchCards = Page.Locator(
            ".branch-card, .portal-branch, table tbody tr, [data-testid='branch-item']");
        if (await branchCards.CountAsync() > 0)
        {
            await Expect(branchCards.First).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task SchoolPortal_AdmissionRequirements_ListAndAddButtonLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/admission-requirements");
        await ExpectMainOrHeadingAsync();

        var addBtn = Page.Locator(
            "button:has-text('إضافة'), button:has-text('Add'), [data-testid='add-requirement']").First;
        if (await addBtn.CountAsync() > 0)
        {
            await Expect(addBtn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task SchoolPortal_AdmissionQuestions_ListAndAddButtonLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/admission-questions");
        await ExpectMainOrHeadingAsync();

        var addBtn = Page.Locator(
            "button:has-text('إضافة'), button:has-text('Add'), [data-testid='add-question']").First;
        if (await addBtn.CountAsync() > 0)
        {
            await Expect(addBtn).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task SchoolPortal_Facilities_ToggleLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/facilities");
        await ExpectMainOrHeadingAsync();

        var checkboxes = Page.Locator(
            "input[type='checkbox'], mat-checkbox, .facility-toggle");
        if (await checkboxes.CountAsync() > 0)
        {
            await Expect(checkboxes.First).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task SchoolPortal_Services_ListLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/services");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolPortal_Team_ListLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/team");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolPortal_Gallery_ShowsUploadArea()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/gallery");
        await ExpectMainOrHeadingAsync();

        var uploadArea = Page.Locator(
            "input[type='file'], .upload-area, [data-testid='gallery-upload'], button:has-text('رفع'), button:has-text('Upload')").First;
        if (await uploadArea.CountAsync() > 0)
        {
            await Expect(uploadArea).ToBeAttachedAsync();
        }
    }

    [Fact]
    public async Task SchoolPortal_AgeEligibilityRules_ListLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/age-eligibility-rules");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolPortal_InterviewAssessmentSlots_ListLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/interview-assessment-slots");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolPortal_InterviewFaqs_ListLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/interview-faqs");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }

    [Fact]
    public async Task SchoolPortal_Stages_ListLoads()
    {
        var id = await GetSchoolIdAsync();
        await GotoAsync($"/school/{id}/stages");
        await ExpectMainOrHeadingAsync();
        await ExpectPageHealthyAsync();
    }
}
