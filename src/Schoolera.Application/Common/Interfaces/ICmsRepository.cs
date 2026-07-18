using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ICmsRepository
{
    Task<CmsPage?> GetPageByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CmsPage?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<CmsPage?> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> IsPageSlugTakenAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<CmsPage>> ListPagesAsync(
        string? search,
        CmsPublicationStatus? status,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task AddPageAsync(CmsPage page, CancellationToken cancellationToken = default);

    Task<FaqCategory?> GetFaqCategoryByIdAsync(
        Guid id,
        bool includeItems = false,
        CancellationToken cancellationToken = default);

    Task<FaqCategory?> GetFaqCategoryBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists FAQ categories. When <paramref name="generalCmsItemsOnly"/> is true (default for public/admin general views),
    /// items with InterviewCategory set or School ownership are excluded from the included Items collection.
    /// </summary>
    Task<IReadOnlyList<FaqCategory>> ListFaqCategoriesAsync(
        bool publishedOnly,
        bool generalCmsItemsOnly = true,
        CancellationToken cancellationToken = default);

    Task<bool> IsFaqCategorySlugTakenAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddFaqCategoryAsync(FaqCategory category, CancellationToken cancellationToken = default);

    Task<int> GetNextFaqCategorySortOrderAsync(CancellationToken cancellationToken = default);

    Task<FaqItem?> GetFaqItemByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FaqItem?> GetFaqItemByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqItem>> GetFaqItemsByCategoryIdAsync(
        Guid categoryId,
        bool generalCmsItemsOnly = true,
        CancellationToken cancellationToken = default);

    Task AddFaqItemAsync(FaqItem item, CancellationToken cancellationToken = default);

    Task<int> GetNextFaqItemSortOrderAsync(
        Guid categoryId,
        FaqOwnershipScope? ownershipScope = null,
        Guid? schoolId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqItem>> ListInterviewFaqsAsync(
        Guid? schoolId,
        InterviewFaqCategory? interviewCategory,
        bool publishedOnly,
        bool activeOnly,
        Guid? branchId = null,
        Guid? stageId = null,
        Guid? gradeId = null,
        Guid? yearId = null,
        bool? isPublished = null,
        bool? isActive = null,
        string? search = null,
        FaqOwnershipScope? ownershipScope = null,
        CancellationToken cancellationToken = default);

    Task<FaqItem?> GetSchoolFaqItemAsync(
        Guid schoolId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqItem>> ListSchoolInterviewFaqsAsync(
        Guid schoolId,
        InterviewFaqCategory? interviewCategory,
        bool? isPublished,
        bool? isActive,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? yearId,
        string? search,
        CancellationToken cancellationToken = default);

    Task<HomepageContent?> GetHomepageByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HomepageContent?> GetLatestHomepageDraftOrSingleAsync(CancellationToken cancellationToken = default);

    Task<HomepageContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default);

    Task AddHomepageAsync(HomepageContent content, CancellationToken cancellationToken = default);

    Task AddContactRequestAsync(ContactRequest request, CancellationToken cancellationToken = default);

    Task<ContactRequest?> GetContactRequestByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasRecentContactDuplicateAsync(
        string? email,
        string phone,
        string subject,
        TimeSpan window,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ContactRequest>> ListContactRequestsAsync(
        string? search,
        ContactRequestStatus? status,
        string? category,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    Task<string> GenerateContactReferenceAsync(CancellationToken cancellationToken = default);
}
