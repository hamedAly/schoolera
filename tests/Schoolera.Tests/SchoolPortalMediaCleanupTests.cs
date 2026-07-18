using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolPortal.Options;
using Schoolera.Infrastructure.Storage;

namespace Schoolera.Tests;

public sealed class SchoolPortalMediaCleanupTests : IDisposable
{
    private readonly string _root;

    public SchoolPortalMediaCleanupTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "schoolera-portal-media-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task CleanupOrphansAsync_DryRun_CountsButDoesNotDelete()
    {
        var referencedPath = WriteFile(SchoolPortalMediaOptions.LogosCategory, "referenced.png");
        var orphanPath = WriteFile(SchoolPortalMediaOptions.LogosCategory, "orphan.png");
        var referencedUrl = ToPublicUrl(referencedPath);

        var cleanup = CreateCleanup([referencedUrl]);

        var result = await cleanup.CleanupOrphansAsync(dryRun: true);

        Assert.Equal(2, result.ScannedFiles);
        Assert.Equal(1, result.OrphanFiles);
        Assert.Equal(0, result.DeletedFiles);
        Assert.True(result.DryRun);
        Assert.True(File.Exists(referencedPath));
        Assert.True(File.Exists(orphanPath));
    }

    [Fact]
    public async Task CleanupOrphansAsync_DeleteMode_RemovesOnlyOrphans()
    {
        var referencedPath = WriteFile(SchoolPortalMediaOptions.CoversCategory, "keep.png");
        var orphanPath = WriteFile(SchoolPortalMediaOptions.CoversCategory, "remove.png");
        var referencedUrl = ToPublicUrl(referencedPath);

        var cleanup = CreateCleanup([referencedUrl]);

        var result = await cleanup.CleanupOrphansAsync(dryRun: false);

        Assert.Equal(2, result.ScannedFiles);
        Assert.Equal(1, result.OrphanFiles);
        Assert.Equal(1, result.DeletedFiles);
        Assert.False(result.DryRun);
        Assert.True(File.Exists(referencedPath));
        Assert.False(File.Exists(orphanPath));
    }

    [Fact]
    public async Task CleanupOrphansAsync_MissingCategoryDirectory_ReturnsZeroCounts()
    {
        var cleanup = CreateCleanup([]);

        var result = await cleanup.CleanupOrphansAsync(dryRun: false);

        Assert.Equal(0, result.ScannedFiles);
        Assert.Equal(0, result.OrphanFiles);
        Assert.Equal(0, result.DeletedFiles);
    }

    private SchoolPortalMediaOrphanCleanup CreateCleanup(IEnumerable<string> referencedUrls)
    {
        var options = new FileStorageOptions
        {
            StorageRoot = _root,
            PublicRequestPath = "/uploads",
        };

        return new SchoolPortalMediaOrphanCleanup(
            new ReferencedMediaRepository(referencedUrls),
            Options.Create(options),
            NullLogger<SchoolPortalMediaOrphanCleanup>.Instance);
    }

    private string WriteFile(string category, string fileName)
    {
        var directory = Path.Combine(_root, category.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "test");
        return path;
    }

    private string ToPublicUrl(string physicalPath)
    {
        var relative = Path.GetRelativePath(_root, physicalPath).Replace('\\', '/');
        return $"/uploads/{relative}";
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class ReferencedMediaRepository(IEnumerable<string> referencedUrls) : ISchoolPortalRepository
    {
        private readonly HashSet<string> _referenced = referencedUrls.ToHashSet(StringComparer.Ordinal);

        public Task<IReadOnlySet<string>> GetReferencedMediaUrlsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(_referenced);

        public Task<IReadOnlyList<AccessibleSchoolRow>> ListAccessibleSchoolsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SchoolAccessSnapshot?> GetAccessSnapshotAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.School?> GetSchoolForWriteAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.School?> GetSchoolProfileAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolBranch>> ListBranchesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolBranch?> GetBranchForWriteAsync(
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

        public Task<IReadOnlyList<Domain.Entities.SchoolStageOffering>> ListOfferingsAsync(
            Guid schoolId,
            Guid? branchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolStageOffering?> GetOfferingForWriteAsync(
            Guid schoolId,
            Guid offeringId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DuplicateOfferingExistsAsync(
            Guid branchId,
            Guid educationalStageId,
            Domain.Enums.GenderType genderType,
            Guid? excludeOfferingId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.TuitionFee>> ListTuitionFeesAsync(
            Guid schoolId,
            Guid? branchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.TuitionFee?> GetTuitionFeeForWriteAsync(
            Guid schoolId,
            Guid feeId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DuplicateFeeExistsAsync(
            Guid branchId,
            Guid educationalStageId,
            Guid? gradeId,
            Guid academicYearId,
            Domain.Enums.FeeCategory category,
            Guid? excludeFeeId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolFeeInstallmentDisplay?> GetInstallmentForWriteAsync(
            Guid schoolId, Guid feeId, Guid installmentId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddInstallmentAsync(
            Domain.Entities.SchoolFeeInstallmentDisplay installment, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolPublishedDiscount>> ListPublishedDiscountsAsync(
            Guid schoolId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolPublishedDiscount?> GetPublishedDiscountForWriteAsync(
            Guid schoolId, Guid discountId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddPublishedDiscountAsync(
            Domain.Entities.SchoolPublishedDiscount discount, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolFinancialNote>> ListFinancialNotesAsync(
            Guid schoolId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolFinancialNote?> GetFinancialNoteForWriteAsync(
            Guid schoolId, Guid noteId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddFinancialNoteAsync(
            Domain.Entities.SchoolFinancialNote note, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolFacility>> ListSchoolFacilitiesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.Facility>> GetActiveFacilitiesByIdsAsync(
            IReadOnlyList<Guid> facilityIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ReplaceSchoolFacilitiesAsync(
            Guid schoolId,
            IReadOnlyList<Guid> facilityIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolImage>> ListImagesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolImage?> GetImageForWriteAsync(
            Guid schoolId,
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> CountGalleryImagesAsync(
            Guid schoolId,
            Guid? educationalStageId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolAdditionalService>> ListServicesAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolAdditionalService?> GetServiceForWriteAsync(
            Guid schoolId,
            Guid serviceId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolTeamMember>> ListActiveTeamMembersAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Domain.Entities.SchoolTeamMember>> ListTeamMembersAsync(
            Guid schoolId,
            bool activeOnly,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolTeamMember?> GetTeamMemberForWriteAsync(
            Guid schoolId,
            Guid membershipId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Domain.Entities.SchoolTeamMember?> GetTeamMemberByUserForWriteAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ActiveTeamMemberExistsAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> BranchesBelongToSchoolAsync(
            Guid schoolId,
            IReadOnlyList<Guid> branchIds,
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

        public Task<Domain.Entities.School?> GetSchoolBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
