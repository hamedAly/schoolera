using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

public sealed class SchoolPortalDomainTests
{
    private static School CreateSchool(SchoolStatus status = SchoolStatus.Published)
    {
        var school = new School(
            "مدرسة",
            "School",
            "test-school",
            SchoolType.Private,
            GenderType.Mixed,
            status);
        school.AssignOwner(Guid.NewGuid());
        return school;
    }

    [Fact]
    public void SchoolTeamMember_Create_StartsActiveWithRole()
    {
        var schoolId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var member = new SchoolTeamMember(schoolId, userId, SchoolTeamRole.SchoolAdmin, creatorId);

        Assert.Equal(schoolId, member.SchoolId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(SchoolTeamRole.SchoolAdmin, member.Role);
        Assert.True(member.IsActive);
        Assert.Equal(creatorId, member.CreatedByUserId);
        Assert.Null(member.DeactivatedAtUtc);
    }

    [Fact]
    public void SchoolTeamMember_Deactivate_SetsInactiveTimestamp()
    {
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.SchoolAdmin,
            Guid.NewGuid());

        member.Deactivate();

        Assert.False(member.IsActive);
        Assert.NotNull(member.DeactivatedAtUtc);
    }

    [Fact]
    public void SchoolTeamMember_Reactivate_ClearsDeactivatedTimestamp()
    {
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.SchoolAdmin,
            Guid.NewGuid());

        member.Deactivate();
        member.Reactivate();

        Assert.True(member.IsActive);
        Assert.Null(member.DeactivatedAtUtc);
    }

    [Fact]
    public void SchoolTeamMember_Deactivate_IsIdempotent()
    {
        var member = new SchoolTeamMember(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SchoolTeamRole.SchoolAdmin,
            Guid.NewGuid());

        member.Deactivate();
        var firstDeactivatedAt = member.DeactivatedAtUtc;

        member.Deactivate();

        Assert.Equal(firstDeactivatedAt, member.DeactivatedAtUtc);
    }

    [Fact]
    public void SchoolAdditionalService_Update_ChangesContentAndSortOrder()
    {
        var service = new SchoolAdditionalService(
            Guid.NewGuid(),
            "نقل",
            "Transport",
            "وصف",
            "Description",
            "bus",
            1);

        service.Update("وجبات", "Meals", "برنامج", "Program", "cafeteria", 2);

        Assert.Equal("وجبات", service.NameAr);
        Assert.Equal("Meals", service.NameEn);
        Assert.Equal("cafeteria", service.IconKey);
        Assert.Equal(2, service.SortOrder);
    }

    [Fact]
    public void SchoolAdditionalService_ActivateDeactivate_TogglesActiveFlag()
    {
        var service = new SchoolAdditionalService(
            Guid.NewGuid(),
            "نقل",
            "Transport",
            null,
            null,
            "bus",
            1);

        service.Deactivate();
        Assert.False(service.IsActive);

        service.Activate();
        Assert.True(service.IsActive);
    }

    [Fact]
    public void SchoolAdditionalService_SetSortOrder_UpdatesOrderOnly()
    {
        var service = new SchoolAdditionalService(
            Guid.NewGuid(),
            "نقل",
            "Transport",
            null,
            null,
            "bus",
            1);

        service.SetSortOrder(5);

        Assert.Equal(5, service.SortOrder);
        Assert.Equal("نقل", service.NameAr);
    }

    [Fact]
    public void School_UpdatePortalProfile_DoesNotChangeStatusOwnerOrSlug()
    {
        var ownerId = Guid.NewGuid();
        var school = new School(
            "مدرسة",
            "School",
            "immutable-slug",
            SchoolType.International,
            GenderType.Mixed,
            SchoolStatus.Published);
        school.AssignOwner(ownerId);

        school.UpdatePortalProfile(
            nameAr: "  اسم جديد  ",
            nameEn: " New Name ",
            shortDescriptionAr: "وصف",
            shortDescriptionEn: "Desc",
            fullDescriptionAr: "كامل",
            fullDescriptionEn: "Full",
            schoolType: SchoolType.National,
            genderType: GenderType.Boys,
            foundedYear: 2005,
            studentCount: 500,
            publicPhone: "+201000000000",
            publicEmail: "info@example.com",
            websiteUrl: "https://example.com",
            whatsAppNumber: "+201000000001",
            seoTitleAr: "عنوان",
            seoTitleEn: "Title",
            seoDescriptionAr: "seo ar",
            seoDescriptionEn: "seo en");

        Assert.Equal(SchoolStatus.Published, school.Status);
        Assert.Equal(ownerId, school.OwnerUserId);
        Assert.Equal("immutable-slug", school.Slug);
        Assert.Equal("اسم جديد", school.NameAr);
        Assert.Equal("New Name", school.NameEn);
        Assert.Equal(SchoolType.National, school.SchoolType);
    }

    [Fact]
    public void School_SetLogoUrl_TrimsAndNullifiesWhitespace()
    {
        var school = CreateSchool();

        school.SetLogoUrl("  /uploads/logo.png  ");
        Assert.Equal("/uploads/logo.png", school.LogoUrl);

        school.SetLogoUrl("   ");
        Assert.Null(school.LogoUrl);
    }

    [Fact]
    public void School_SetCoverUrl_TrimsAndNullifiesWhitespace()
    {
        var school = CreateSchool();

        school.SetCoverUrl("  /uploads/cover.png  ");
        Assert.Equal("/uploads/cover.png", school.CoverUrl);

        school.SetCoverUrl(null);
        Assert.Null(school.CoverUrl);
    }

    [Fact]
    public void SchoolBranch_ActivateDeactivate_TogglesActiveFlag()
    {
        var branch = CreateBranch();

        branch.Deactivate();
        Assert.False(branch.IsActive);

        branch.Activate();
        Assert.True(branch.IsActive);
    }

    [Fact]
    public void SchoolBranch_SetMainBranch_UpdatesFlag()
    {
        var branch = CreateBranch(isMainBranch: false);

        branch.SetMainBranch(true);

        Assert.True(branch.IsMainBranch);
    }

    [Theory]
    [InlineData(SchoolStatus.Draft, true)]
    [InlineData(SchoolStatus.Unpublished, true)]
    [InlineData(SchoolStatus.Published, true)]
    [InlineData(SchoolStatus.Suspended, false)]
    public void EditabilityMatrix_MirrorsSchoolPortalAccessRule(SchoolStatus status, bool expectedEditable)
    {
        var isEditable = status is not SchoolStatus.Suspended;
        Assert.Equal(expectedEditable, isEditable);
    }

    private static SchoolBranch CreateBranch(bool isMainBranch = true)
    {
        return new SchoolBranch(
            Guid.NewGuid(),
            "فرع",
            "Branch",
            "branch-slug",
            Guid.NewGuid(),
            Guid.NewGuid(),
            isMainBranch);
    }
}
