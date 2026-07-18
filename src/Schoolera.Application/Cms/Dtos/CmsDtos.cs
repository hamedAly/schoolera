using Schoolera.Application.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Dtos;

public sealed record PublicCmsPageDto(
    string Slug,
    string Title,
    string Content,
    string? MetaTitle,
    string? MetaDescription);

public sealed record PublicFaqCategoryDto(
    string Slug,
    string Name,
    IReadOnlyList<PublicFaqItemDto> Items);

public sealed record PublicFaqItemDto(
    Guid Id,
    string Question,
    string Answer);

public sealed record PublicHomepageDto(
    string HeroTitle,
    string HeroSubtitle,
    string PrimaryCtaLabel,
    string PrimaryCtaUrl,
    string? SecondaryCtaLabel,
    string? SecondaryCtaUrl,
    string SchoolsSectionTitle,
    string ParentJourneyTitle,
    string ParentJourneyText,
    string SchoolJourneyTitle,
    string SchoolJourneyText,
    string FaqSectionTitle,
    string? FaqSectionSubtitle);

public sealed record ContactRequestResultDto(string Reference);

public sealed record ContactAdminNoteBody(string? AdminNote);

public sealed record ContactRequestBody(
    string Name,
    string Phone,
    string? Email,
    string Category,
    string Subject,
    string Message,
    bool ConsentAccepted,
    string Source,
    string? Website);

public sealed record CmsPageAdminDto(
    Guid Id,
    string Slug,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    string? MetaTitleAr,
    string? MetaTitleEn,
    string? MetaDescriptionAr,
    string? MetaDescriptionEn,
    CmsPublicationStatus Status,
    bool IsSystemPage,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion)
{
    public static CmsPageAdminDto FromEntity(CmsPage page) =>
        new(
            page.Id,
            page.Slug,
            page.TitleAr,
            page.TitleEn,
            page.ContentAr,
            page.ContentEn,
            page.MetaTitleAr,
            page.MetaTitleEn,
            page.MetaDescriptionAr,
            page.MetaDescriptionEn,
            page.Status,
            page.IsSystemPage,
            page.PublishedAtUtc,
            page.CreatedAtUtc,
            page.UpdatedAtUtc,
            page.RowVersion);
}

public sealed record CmsPageListItemDto(
    Guid Id,
    string Slug,
    string TitleAr,
    string TitleEn,
    CmsPublicationStatus Status,
    bool IsSystemPage,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record FaqCategoryAdminDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string Slug,
    int SortOrder,
    bool IsPublished,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FaqItemAdminDto> Items)
{
    public static FaqCategoryAdminDto FromEntity(FaqCategory category, IReadOnlyList<FaqItem> items) =>
        new(
            category.Id,
            category.NameAr,
            category.NameEn,
            category.Slug,
            category.SortOrder,
            category.IsPublished,
            category.CreatedAtUtc,
            category.UpdatedAtUtc,
            items.Select(FaqItemAdminDto.FromEntity).ToArray());
}

public sealed record FaqItemAdminDto(
    Guid Id,
    Guid FaqCategoryId,
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    int SortOrder,
    bool IsPublished,
    bool IsActive,
    FaqOwnershipScope OwnershipScope,
    Guid? SchoolId,
    InterviewFaqCategory? InterviewCategory,
    Guid? SchoolBranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion)
{
    public static FaqItemAdminDto FromEntity(FaqItem item) =>
        new(
            item.Id,
            item.FaqCategoryId,
            item.QuestionAr,
            item.QuestionEn,
            item.AnswerAr,
            item.AnswerEn,
            item.SortOrder,
            item.IsPublished,
            item.IsActive,
            item.OwnershipScope,
            item.SchoolId,
            item.InterviewCategory,
            item.SchoolBranchId,
            item.EducationalStageId,
            item.GradeId,
            item.AcademicYearId,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            item.RowVersion);
}

public sealed record PublicInterviewFaqItemDto(
    Guid Id,
    InterviewFaqCategory Category,
    FaqOwnershipScope OwnershipScope,
    string Question,
    string Answer,
    int SortOrder,
    DateTimeOffset UpdatedAtUtc)
{
    public static PublicInterviewFaqItemDto FromEntity(FaqItem item) =>
        new(
            item.Id,
            item.InterviewCategory ?? InterviewFaqCategory.Interview,
            item.OwnershipScope,
            LocalizationDisplayHelper.Pick(item.QuestionAr, item.QuestionEn),
            LocalizationDisplayHelper.Pick(item.AnswerAr, item.AnswerEn),
            item.SortOrder,
            item.UpdatedAtUtc);
}

public sealed record HomepageAdminDto(
    Guid Id,
    string HeroTitleAr,
    string HeroTitleEn,
    string HeroSubtitleAr,
    string HeroSubtitleEn,
    string PrimaryCtaLabelAr,
    string PrimaryCtaLabelEn,
    string PrimaryCtaUrl,
    string? SecondaryCtaLabelAr,
    string? SecondaryCtaLabelEn,
    string? SecondaryCtaUrl,
    string SchoolsSectionTitleAr,
    string SchoolsSectionTitleEn,
    string ParentJourneyTitleAr,
    string ParentJourneyTitleEn,
    string ParentJourneyTextAr,
    string ParentJourneyTextEn,
    string SchoolJourneyTitleAr,
    string SchoolJourneyTitleEn,
    string SchoolJourneyTextAr,
    string SchoolJourneyTextEn,
    string FaqSectionTitleAr,
    string FaqSectionTitleEn,
    string? FaqSectionSubtitleAr,
    string? FaqSectionSubtitleEn,
    CmsPublicationStatus Status,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    byte[] RowVersion)
{
    public static HomepageAdminDto FromEntity(HomepageContent content) =>
        new(
            content.Id,
            content.HeroTitleAr,
            content.HeroTitleEn,
            content.HeroSubtitleAr,
            content.HeroSubtitleEn,
            content.PrimaryCtaLabelAr,
            content.PrimaryCtaLabelEn,
            content.PrimaryCtaUrl,
            content.SecondaryCtaLabelAr,
            content.SecondaryCtaLabelEn,
            content.SecondaryCtaUrl,
            content.SchoolsSectionTitleAr,
            content.SchoolsSectionTitleEn,
            content.ParentJourneyTitleAr,
            content.ParentJourneyTitleEn,
            content.ParentJourneyTextAr,
            content.ParentJourneyTextEn,
            content.SchoolJourneyTitleAr,
            content.SchoolJourneyTitleEn,
            content.SchoolJourneyTextAr,
            content.SchoolJourneyTextEn,
            content.FaqSectionTitleAr,
            content.FaqSectionTitleEn,
            content.FaqSectionSubtitleAr,
            content.FaqSectionSubtitleEn,
            content.Status,
            content.PublishedAtUtc,
            content.CreatedAtUtc,
            content.UpdatedAtUtc,
            content.RowVersion);
}

public sealed record ContactRequestListItemDto(
    Guid Id,
    string Reference,
    string Name,
    string Phone,
    string? Email,
    string Category,
    string Subject,
    string Source,
    ContactRequestStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ContactRequestDetailDto(
    Guid Id,
    string Reference,
    string Name,
    string Phone,
    string? Email,
    string Category,
    string Subject,
    string Message,
    bool ConsentAccepted,
    string Source,
    ContactRequestStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    string? AdminNote);

public static class CmsDtoMapping
{
    public static PublicCmsPageDto ToPublicPage(CmsPage page) =>
        new(
            page.Slug,
            LocalizationDisplayHelper.Pick(page.TitleAr, page.TitleEn),
            LocalizationDisplayHelper.Pick(page.ContentAr, page.ContentEn),
            LocalizationDisplayHelper.Pick(page.MetaTitleAr, page.MetaTitleEn),
            LocalizationDisplayHelper.Pick(page.MetaDescriptionAr, page.MetaDescriptionEn));

    public static PublicFaqCategoryDto ToPublicCategory(FaqCategory category, IReadOnlyList<FaqItem> items) =>
        new(
            category.Slug,
            LocalizationDisplayHelper.Pick(category.NameAr, category.NameEn),
            items
                .Where(item =>
                    item.IsPublished &&
                    item.IsActive &&
                    item.InterviewCategory is null &&
                    item.OwnershipScope == FaqOwnershipScope.Platform)
                .OrderBy(item => item.SortOrder)
                .Select(item => new PublicFaqItemDto(
                    item.Id,
                    LocalizationDisplayHelper.Pick(item.QuestionAr, item.QuestionEn),
                    LocalizationDisplayHelper.Pick(item.AnswerAr, item.AnswerEn)))
                .ToArray());

    public static PublicHomepageDto ToPublicHomepage(HomepageContent content) =>
        new(
            LocalizationDisplayHelper.Pick(content.HeroTitleAr, content.HeroTitleEn),
            LocalizationDisplayHelper.Pick(content.HeroSubtitleAr, content.HeroSubtitleEn),
            LocalizationDisplayHelper.Pick(content.PrimaryCtaLabelAr, content.PrimaryCtaLabelEn),
            content.PrimaryCtaUrl,
            LocalizationDisplayHelper.Pick(content.SecondaryCtaLabelAr, content.SecondaryCtaLabelEn),
            content.SecondaryCtaUrl,
            LocalizationDisplayHelper.Pick(content.SchoolsSectionTitleAr, content.SchoolsSectionTitleEn),
            LocalizationDisplayHelper.Pick(content.ParentJourneyTitleAr, content.ParentJourneyTitleEn),
            LocalizationDisplayHelper.Pick(content.ParentJourneyTextAr, content.ParentJourneyTextEn),
            LocalizationDisplayHelper.Pick(content.SchoolJourneyTitleAr, content.SchoolJourneyTitleEn),
            LocalizationDisplayHelper.Pick(content.SchoolJourneyTextAr, content.SchoolJourneyTextEn),
            LocalizationDisplayHelper.Pick(content.FaqSectionTitleAr, content.FaqSectionTitleEn),
            LocalizationDisplayHelper.Pick(content.FaqSectionSubtitleAr, content.FaqSectionSubtitleEn));

    public static ContactRequestListItemDto ToListItem(ContactRequest request) =>
        new(
            request.Id,
            request.Reference,
            request.Name,
            request.Phone,
            request.Email,
            request.Category,
            request.Subject,
            request.Source,
            request.Status,
            request.CreatedAtUtc,
            request.UpdatedAtUtc);

    public static ContactRequestDetailDto ToDetail(ContactRequest request) =>
        new(
            request.Id,
            request.Reference,
            request.Name,
            request.Phone,
            request.Email,
            request.Category,
            request.Subject,
            request.Message,
            request.ConsentAccepted,
            request.Source,
            request.Status,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.ReviewedAtUtc,
            request.ReviewedByUserId,
            request.AdminNote);
}
