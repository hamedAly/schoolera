using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class CmsRepository(SchooleraDbContext dbContext) : ICmsRepository
{
    public Task<CmsPage?> GetPageByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.CmsPages.FirstOrDefaultAsync(page => page.Id == id, cancellationToken);

    public Task<CmsPage?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return dbContext.CmsPages.FirstOrDefaultAsync(page => page.Slug == normalized, cancellationToken);
    }

    public Task<CmsPage?> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return dbContext.CmsPages.AsNoTracking()
            .FirstOrDefaultAsync(
                page => page.Slug == normalized && page.Status == CmsPublicationStatus.Published,
                cancellationToken);
    }

    public Task<bool> IsPageSlugTakenAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var query = dbContext.CmsPages.AsNoTracking().Where(page => page.Slug == normalized);
        if (excludeId.HasValue)
        {
            query = query.Where(page => page.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<PagedResult<CmsPage>> ListPagesAsync(
        string? search,
        CmsPublicationStatus? status,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CmsPages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(page =>
                page.Slug.Contains(term) ||
                page.TitleAr.Contains(term) ||
                page.TitleEn.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(page => page.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(page => page.UpdatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<CmsPage>.Create(items, totalCount, paging);
    }

    public async Task AddPageAsync(CmsPage page, CancellationToken cancellationToken = default) =>
        await dbContext.CmsPages.AddAsync(page, cancellationToken);

    public Task<FaqCategory?> GetFaqCategoryByIdAsync(
        Guid id,
        bool includeItems = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FaqCategories.AsQueryable();
        if (includeItems)
        {
            query = query.Include(category => category.Items);
        }

        return query.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public Task<FaqCategory?> GetFaqCategoryBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return dbContext.FaqCategories
            .FirstOrDefaultAsync(category => category.Slug == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<FaqCategory>> ListFaqCategoriesAsync(
        bool publishedOnly,
        bool generalCmsItemsOnly = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<FaqCategory> query = dbContext.FaqCategories.AsNoTracking();
        if (generalCmsItemsOnly)
        {
            query = query.Include(category => category.Items
                .Where(item =>
                    item.InterviewCategory == null &&
                    item.OwnershipScope == FaqOwnershipScope.Platform));
        }
        else
        {
            query = query.Include(category => category.Items);
        }

        if (publishedOnly)
        {
            query = query.Where(category => category.IsPublished);
        }

        return await query
            .OrderBy(category => category.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsFaqCategorySlugTakenAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var query = dbContext.FaqCategories.AsNoTracking().Where(category => category.Slug == normalized);
        if (excludeId.HasValue)
        {
            query = query.Where(category => category.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddFaqCategoryAsync(FaqCategory category, CancellationToken cancellationToken = default) =>
        await dbContext.FaqCategories.AddAsync(category, cancellationToken);

    public async Task<int> GetNextFaqCategorySortOrderAsync(CancellationToken cancellationToken = default)
    {
        var max = await dbContext.FaqCategories.MaxAsync(category => (int?)category.SortOrder, cancellationToken);
        return (max ?? 0) + 1;
    }

    public Task<FaqItem?> GetFaqItemByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.FaqItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<FaqItem?> GetFaqItemByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.FaqItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FaqItem>> GetFaqItemsByCategoryIdAsync(
        Guid categoryId,
        bool generalCmsItemsOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FaqItems.AsNoTracking()
            .Where(item => item.FaqCategoryId == categoryId);

        if (generalCmsItemsOnly)
        {
            query = query.Where(item =>
                item.InterviewCategory == null &&
                item.OwnershipScope == FaqOwnershipScope.Platform);
        }

        return await query
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task AddFaqItemAsync(FaqItem item, CancellationToken cancellationToken = default) =>
        await dbContext.FaqItems.AddAsync(item, cancellationToken);

    public async Task<int> GetNextFaqItemSortOrderAsync(
        Guid categoryId,
        FaqOwnershipScope? ownershipScope = null,
        Guid? schoolId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FaqItems.AsQueryable();

        if (ownershipScope == FaqOwnershipScope.School && schoolId is { } sid)
        {
            query = query.Where(item =>
                item.OwnershipScope == FaqOwnershipScope.School &&
                item.SchoolId == sid);
        }
        else if (ownershipScope == FaqOwnershipScope.Platform)
        {
            query = query.Where(item =>
                item.OwnershipScope == FaqOwnershipScope.Platform &&
                item.FaqCategoryId == categoryId);
        }
        else
        {
            query = query.Where(item => item.FaqCategoryId == categoryId);
        }

        var max = await query.MaxAsync(item => (int?)item.SortOrder, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task<IReadOnlyList<FaqItem>> ListInterviewFaqsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FaqItems.AsNoTracking()
            .Where(item => item.InterviewCategory != null);

        if (ownershipScope.HasValue)
        {
            query = query.Where(item => item.OwnershipScope == ownershipScope.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(item =>
                item.OwnershipScope == FaqOwnershipScope.Platform ||
                (item.OwnershipScope == FaqOwnershipScope.School && item.SchoolId == schoolId.Value));
        }
        else if (ownershipScope == FaqOwnershipScope.School)
        {
            query = query.Where(item => item.OwnershipScope == FaqOwnershipScope.School);
        }

        if (interviewCategory.HasValue)
        {
            var category = interviewCategory.Value;
            query = query.Where(item =>
                item.InterviewCategory == category ||
                (item.InterviewCategory == InterviewFaqCategory.InterviewAndAssessment &&
                 (category == InterviewFaqCategory.Interview || category == InterviewFaqCategory.Assessment)));
        }

        if (isPublished.HasValue)
        {
            query = query.Where(item => item.IsPublished == isPublished.Value);
        }
        else if (publishedOnly)
        {
            query = query.Where(item => item.IsPublished);
        }

        if (isActive.HasValue)
        {
            query = query.Where(item => item.IsActive == isActive.Value);
        }
        else if (activeOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.QuestionAr.Contains(term) ||
                item.QuestionEn.Contains(term) ||
                item.AnswerAr.Contains(term) ||
                item.AnswerEn.Contains(term));
        }

        if (branchId.HasValue)
        {
            query = query.Where(item =>
                item.OwnershipScope == FaqOwnershipScope.Platform ||
                item.SchoolBranchId == null ||
                item.SchoolBranchId == branchId.Value);
        }

        if (stageId.HasValue)
        {
            query = query.Where(item =>
                item.OwnershipScope == FaqOwnershipScope.Platform ||
                item.EducationalStageId == null ||
                item.EducationalStageId == stageId.Value);
        }

        if (gradeId.HasValue)
        {
            query = query.Where(item =>
                item.OwnershipScope == FaqOwnershipScope.Platform ||
                item.GradeId == null ||
                item.GradeId == gradeId.Value);
        }

        if (yearId.HasValue)
        {
            query = query.Where(item =>
                item.OwnershipScope == FaqOwnershipScope.Platform ||
                item.AcademicYearId == null ||
                item.AcademicYearId == yearId.Value);
        }

        var items = await query
            .OrderBy(item => item.OwnershipScope)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<FaqItem?> GetSchoolFaqItemAsync(
        Guid schoolId,
        Guid itemId,
        CancellationToken cancellationToken = default) =>
        dbContext.FaqItems.FirstOrDefaultAsync(
            item =>
                item.Id == itemId &&
                item.OwnershipScope == FaqOwnershipScope.School &&
                item.SchoolId == schoolId,
            cancellationToken);

    public async Task<IReadOnlyList<FaqItem>> ListSchoolInterviewFaqsAsync(
        Guid schoolId,
        InterviewFaqCategory? interviewCategory,
        bool? isPublished,
        bool? isActive,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? yearId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FaqItems.AsNoTracking()
            .Where(item =>
                item.OwnershipScope == FaqOwnershipScope.School &&
                item.SchoolId == schoolId &&
                item.InterviewCategory != null);

        if (interviewCategory.HasValue)
        {
            query = query.Where(item => item.InterviewCategory == interviewCategory.Value);
        }

        if (isPublished.HasValue)
        {
            query = query.Where(item => item.IsPublished == isPublished.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(item => item.IsActive == isActive.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(item => item.SchoolBranchId == null || item.SchoolBranchId == branchId.Value);
        }

        if (stageId.HasValue)
        {
            query = query.Where(item => item.EducationalStageId == null || item.EducationalStageId == stageId.Value);
        }

        if (gradeId.HasValue)
        {
            query = query.Where(item => item.GradeId == null || item.GradeId == gradeId.Value);
        }

        if (yearId.HasValue)
        {
            query = query.Where(item => item.AcademicYearId == null || item.AcademicYearId == yearId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.QuestionAr.Contains(term) ||
                item.QuestionEn.Contains(term));
        }

        return await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<HomepageContent?> GetHomepageByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.HomepageContents.FirstOrDefaultAsync(content => content.Id == id, cancellationToken);

    public Task<HomepageContent?> GetLatestHomepageDraftOrSingleAsync(CancellationToken cancellationToken = default) =>
        dbContext.HomepageContents
            .OrderByDescending(content => content.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<HomepageContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default) =>
        dbContext.HomepageContents.AsNoTracking()
            .Where(content => content.Status == CmsPublicationStatus.Published)
            .OrderByDescending(content => content.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddHomepageAsync(HomepageContent content, CancellationToken cancellationToken = default) =>
        await dbContext.HomepageContents.AddAsync(content, cancellationToken);

    public async Task AddContactRequestAsync(ContactRequest request, CancellationToken cancellationToken = default) =>
        await dbContext.ContactRequests.AddAsync(request, cancellationToken);

    public Task<ContactRequest?> GetContactRequestByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ContactRequests.FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public Task<bool> HasRecentContactDuplicateAsync(
        string? email,
        string phone,
        string subject,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var since = DateTimeOffset.UtcNow - window;
        var normalizedPhone = phone.Trim();
        var normalizedSubject = subject.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        return dbContext.ContactRequests.AsNoTracking()
            .AnyAsync(
                request =>
                    request.Phone == normalizedPhone &&
                    request.Subject == normalizedSubject &&
                    request.Email == normalizedEmail &&
                    request.CreatedAtUtc >= since,
                cancellationToken);
    }

    public async Task<PagedResult<ContactRequest>> ListContactRequestsAsync(
        string? search,
        ContactRequestStatus? status,
        string? category,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ContactRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(request =>
                request.Reference.Contains(term) ||
                request.Name.Contains(term) ||
                request.Subject.Contains(term) ||
                (request.Email != null && request.Email.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(request => request.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(request => request.Category == normalizedCategory);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(request => request.CreatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<ContactRequest>.Create(items, totalCount, paging);
    }

    public async Task<string> GenerateContactReferenceAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var prefix = $"CNT-{year}-";
        var lastReference = await dbContext.ContactRequests.AsNoTracking()
            .Where(request => request.Reference.StartsWith(prefix))
            .OrderByDescending(request => request.Reference)
            .Select(request => request.Reference)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrWhiteSpace(lastReference) &&
            lastReference.Length > prefix.Length &&
            int.TryParse(lastReference[prefix.Length..], out var parsed))
        {
            sequence = parsed + 1;
        }

        return $"{prefix}{sequence:D4}";
    }
}
