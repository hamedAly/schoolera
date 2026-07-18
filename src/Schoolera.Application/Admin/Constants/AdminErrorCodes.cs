namespace Schoolera.Application.Admin.Constants;

/// <summary>Stable Platform Admin error codes. Frontends branch on these, never on localized text.</summary>
public static class AdminErrorCodes
{
    public const string SchoolNotFound = "admin.schoolNotFound";
    public const string UserNotFound = "admin.userNotFound";
    public const string InvalidSchoolStatus = "admin.invalidSchoolStatus";
    public const string InvalidAccountStatus = "admin.invalidAccountStatus";
    public const string CannotModifySelf = "admin.cannotModifySelf";
    public const string Forbidden = "admin.forbidden";
}

public static class AdminAuditActions
{
    public const string SchoolStatusChanged = "school.status_changed";
    public const string UserStatusChanged = "user.status_changed";
    public const string OnboardingApproved = "onboarding.approved";
    public const string OnboardingRejected = "onboarding.rejected";
    public const string CmsPageCreated = "cms.page_created";
    public const string CmsPageUpdated = "cms.page_updated";
    public const string CmsPagePublished = "cms.page_published";
    public const string CmsPageUnpublished = "cms.page_unpublished";
    public const string CmsPageArchived = "cms.page_archived";
    public const string CmsFaqCategoryCreated = "cms.faq_category_created";
    public const string CmsFaqCategoryUpdated = "cms.faq_category_updated";
    public const string CmsFaqCategoryPublished = "cms.faq_category_published";
    public const string CmsFaqCategoryUnpublished = "cms.faq_category_unpublished";
    public const string CmsFaqCategoryReordered = "cms.faq_category_reordered";
    public const string CmsFaqItemCreated = "cms.faq_item_created";
    public const string CmsFaqItemUpdated = "cms.faq_item_updated";
    public const string CmsFaqItemPublished = "cms.faq_item_published";
    public const string CmsFaqItemUnpublished = "cms.faq_item_unpublished";
    public const string CmsFaqItemReordered = "cms.faq_item_reordered";
    public const string CmsFaqItemActivated = "cms.faq_item_activated";
    public const string CmsFaqItemDeactivated = "cms.faq_item_deactivated";
    public const string CmsHomeUpdated = "cms.home_updated";
    public const string CmsHomePublished = "cms.home_published";
    public const string CmsHomeUnpublished = "cms.home_unpublished";
    public const string ContactStatusChanged = "contact.status_changed";
}
