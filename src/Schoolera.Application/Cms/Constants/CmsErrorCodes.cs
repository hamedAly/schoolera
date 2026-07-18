namespace Schoolera.Application.Cms.Constants;

public static class CmsErrorCodes
{
    public const string PageNotFound = "cms.page.notFound";
    public const string PageSlugReserved = "cms.page.slugReserved";
    public const string PageSlugInvalid = "cms.page.slugInvalid";
    public const string PageSlugDuplicate = "cms.page.slugDuplicate";
    public const string PagePublishValidation = "cms.page.publishValidation";
    public const string PageSystemSlugImmutable = "cms.page.systemSlugImmutable";
    public const string PageConcurrentUpdate = "cms.page.concurrentUpdate";
    public const string FaqNotFound = "cms.faq.notFound";
    public const string FaqCategoryNotPublished = "cms.faq.categoryNotPublished";
    public const string FaqReorderInvalid = "cms.faq.reorderInvalid";
    public const string FaqInvalidOwnership = "cms.faq.invalidOwnership";
    public const string FaqInterviewCategoryRequired = "cms.faq.interviewCategoryRequired";
    public const string HomeNotFound = "cms.home.notFound";
    public const string HomeInvalidCtaUrl = "cms.home.invalidCtaUrl";
    public const string Forbidden = "cms.forbidden";
}
