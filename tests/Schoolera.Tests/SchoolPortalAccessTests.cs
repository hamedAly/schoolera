using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

public sealed class SchoolPortalAccessTests
{
    private readonly StubSchoolPortalMessagesLocalizer _localizer = new();

    [Fact]
    public async Task ResolveAsync_WhenAnonymous_ReturnsSchoolNotFound()
    {
        var access = new SchoolPortalAccess(
            new StubSchoolPortalRepository(),
            new StubCurrentUser(isAuthenticated: false),
            _localizer);

        var result = await access.ResolveAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Contains(SchoolPortalErrorCodes.SchoolNotFound, result.ErrorCodes);
    }

    [Fact]
    public async Task ResolveAsync_WhenUserHasNoAccess_ReturnsSchoolNotFound()
    {
        var access = new SchoolPortalAccess(
            new StubSchoolPortalRepository(snapshot: null),
            new StubCurrentUser(isAuthenticated: true, userId: Guid.NewGuid()),
            _localizer);

        var result = await access.ResolveAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Contains(SchoolPortalErrorCodes.SchoolNotFound, result.ErrorCodes);
    }

    [Fact]
    public async Task ResolveAsync_WhenOwner_GrantsTeamManagementAndAllPermissions()
    {
        var schoolId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var access = new SchoolPortalAccess(
            new StubSchoolPortalRepository(OwnerSnapshot(schoolId, ownerId, SchoolStatus.Published)),
            new StubCurrentUser(isAuthenticated: true, userId: ownerId),
            _localizer);

        var result = await access.ResolveAsync(schoolId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsOwner);
        Assert.Null(result.Data.MembershipRole);
        Assert.True(result.Data.CanManageTeam);
        Assert.True(result.Data.HasPermission(SchoolPortalPermission.ManageTeam));
        Assert.True(result.Data.HasPermission(SchoolPortalPermission.TransferOwnership));
        Assert.True(result.Data.IsEditable);
    }

    [Fact]
    public async Task ResolveAsync_WhenSchoolAdminMembership_GrantsEditButNotTeamManagement()
    {
        var schoolId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var access = new SchoolPortalAccess(
            new StubSchoolPortalRepository(
                MemberSnapshot(schoolId, Guid.NewGuid(), SchoolTeamRole.SchoolAdmin, SchoolStatus.Published)),
            new StubCurrentUser(isAuthenticated: true, userId: adminId),
            _localizer);

        var result = await access.ResolveAsync(schoolId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.IsOwner);
        Assert.Equal(SchoolTeamRole.SchoolAdmin, result.Data.MembershipRole);
        Assert.False(result.Data.CanManageTeam);
        Assert.False(result.Data.HasPermission(SchoolPortalPermission.ManageTeam));
        Assert.True(result.Data.HasPermission(SchoolPortalPermission.ManageProfile));
        Assert.True(result.Data.HasPermission(SchoolPortalPermission.ViewApplications));
    }

    [Fact]
    public async Task ResolveAsync_WhenAdmissionOfficer_HasAdmissionPermissionsOnly()
    {
        var schoolId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var access = new SchoolPortalAccess(
            new StubSchoolPortalRepository(
                MemberSnapshot(
                    schoolId,
                    Guid.NewGuid(),
                    SchoolTeamRole.AdmissionOfficer,
                    SchoolStatus.Published,
                    SchoolBranchScopeMode.SelectedBranches,
                    [branchId])),
            new StubCurrentUser(isAuthenticated: true, userId: userId),
            _localizer);

        var result = await access.ResolveAsync(schoolId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.HasPermission(SchoolPortalPermission.ViewApplications));
        Assert.True(result.Data.HasPermission(SchoolPortalPermission.ManageApplicationReview));
        Assert.False(result.Data.HasPermission(SchoolPortalPermission.ManageFees));
        Assert.False(result.Data.HasPermission(SchoolPortalPermission.ManageGallery));
        Assert.True(result.Data.CanAccessBranch(branchId));
        Assert.False(result.Data.CanAccessBranch(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(SchoolStatus.Draft, true)]
    [InlineData(SchoolStatus.Unpublished, true)]
    [InlineData(SchoolStatus.Published, true)]
    [InlineData(SchoolStatus.Suspended, false)]
    public async Task ResolveAsync_EditabilityFollowsSchoolStatus(SchoolStatus status, bool expectedEditable)
    {
        var schoolId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var access = new SchoolPortalAccess(
            new StubSchoolPortalRepository(OwnerSnapshot(schoolId, ownerId, status)),
            new StubCurrentUser(isAuthenticated: true, userId: ownerId),
            _localizer);

        var result = await access.ResolveAsync(schoolId);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedEditable, result.Data!.IsEditable);
    }

    private static SchoolAccessSnapshot OwnerSnapshot(Guid schoolId, Guid ownerId, SchoolStatus status) =>
        new(schoolId, ownerId, status, false, null, null, SchoolBranchScopeMode.AllBranches, []);

    private static SchoolAccessSnapshot MemberSnapshot(
        Guid schoolId,
        Guid ownerId,
        SchoolTeamRole role,
        SchoolStatus status,
        SchoolBranchScopeMode scope = SchoolBranchScopeMode.AllBranches,
        IReadOnlyList<Guid>? branches = null) =>
        new(
            schoolId,
            ownerId,
            status,
            true,
            Guid.NewGuid(),
            role,
            scope,
            branches ?? Array.Empty<Guid>());

    private sealed class StubCurrentUser(bool isAuthenticated, Guid? userId = null) : ICurrentUser
    {
        public bool IsAuthenticated { get; } = isAuthenticated;
        public Guid? UserId { get; } = userId;
        public bool IsInRole(string role) => false;
    }

    private sealed class StubSchoolPortalMessagesLocalizer : IStringLocalizer<SchoolPortalMessages>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class StubSchoolPortalRepository(SchoolAccessSnapshot? snapshot = null) : ISchoolPortalRepository
    {
        public Task<SchoolAccessSnapshot?> GetAccessSnapshotAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);

        public Task<IReadOnlyList<AccessibleSchoolRow>> ListAccessibleSchoolsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<School?> GetSchoolForWriteAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<School?> GetSchoolProfileAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolBranch>> ListBranchesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolBranch?> GetBranchForWriteAsync(
            Guid schoolId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> BranchSlugExistsAsync(
            Guid schoolId,
            string slug,
            Guid? excludeBranchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> HasOtherActiveMainBranchAsync(
            Guid schoolId,
            Guid excludeBranchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolStageOffering>> ListOfferingsAsync(
            Guid schoolId,
            Guid? branchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolStageOffering?> GetOfferingForWriteAsync(
            Guid schoolId,
            Guid offeringId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DuplicateOfferingExistsAsync(
            Guid branchId,
            Guid educationalStageId,
            GenderType genderType,
            Guid? excludeOfferingId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<TuitionFee>> ListTuitionFeesAsync(
            Guid schoolId,
            Guid? branchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TuitionFee?> GetTuitionFeeForWriteAsync(
            Guid schoolId,
            Guid feeId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DuplicateFeeExistsAsync(
            Guid branchId,
            Guid educationalStageId,
            Guid? gradeId,
            Guid academicYearId,
            FeeCategory category,
            Guid? excludeFeeId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolFeeInstallmentDisplay?> GetInstallmentForWriteAsync(
            Guid schoolId, Guid feeId, Guid installmentId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddInstallmentAsync(
            SchoolFeeInstallmentDisplay installment, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolPublishedDiscount>> ListPublishedDiscountsAsync(
            Guid schoolId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolPublishedDiscount?> GetPublishedDiscountForWriteAsync(
            Guid schoolId, Guid discountId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddPublishedDiscountAsync(
            SchoolPublishedDiscount discount, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolFinancialNote>> ListFinancialNotesAsync(
            Guid schoolId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolFinancialNote?> GetFinancialNoteForWriteAsync(
            Guid schoolId, Guid noteId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddFinancialNoteAsync(
            SchoolFinancialNote note, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolFacility>> ListSchoolFacilitiesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Facility>> GetActiveFacilitiesByIdsAsync(
            IReadOnlyList<Guid> facilityIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ReplaceSchoolFacilitiesAsync(
            Guid schoolId,
            IReadOnlyList<Guid> facilityIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolImage>> ListImagesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolImage?> GetImageForWriteAsync(
            Guid schoolId,
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> CountGalleryImagesAsync(
            Guid schoolId,
            Guid? educationalStageId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolAdditionalService>> ListServicesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolAdditionalService?> GetServiceForWriteAsync(
            Guid schoolId,
            Guid serviceId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SchoolTeamMember>> ListActiveTeamMembersAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolTeamMember?> GetTeamMemberForWriteAsync(
            Guid schoolId,
            Guid membershipId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolTeamMember?> GetTeamMemberByUserForWriteAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ActiveTeamMemberExistsAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DistrictBelongsToCityAsync(
            Guid districtId,
            Guid cityId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> EducationalStageExistsAsync(
            Guid educationalStageId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> GradesBelongToStageAsync(
            Guid educationalStageId,
            IReadOnlyList<Guid> gradeIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> AcademicYearExistsAsync(
            Guid academicYearId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolDashboardCounts> GetDashboardCountsAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlySet<string>> GetReferencedMediaUrlsAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<School?> GetSchoolBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
