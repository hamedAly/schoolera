using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence;

/// <summary>
/// Idempotent CMS / FAQ / homepage / demo contact seed. Skips rows that were edited after creation.
/// </summary>
public sealed class CmsContentSeeder(
    SchooleraDbContext dbContext,
    ILogger<CmsContentSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running CMS content seed...");
        await SeedSystemPagesAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SeedInitialLegalVersionsAsync(cancellationToken);
        await SeedHomepageAsync(cancellationToken);
        await SeedFaqAsync(cancellationToken);
        await SeedDemoContactRequestAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("CMS content seed completed.");
    }

    private async Task SeedInitialLegalVersionsAsync(CancellationToken cancellationToken)
    {
        foreach (var slug in new[] { "terms", "privacy" })
        {
            var page = await dbContext.CmsPages
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Slug == slug, cancellationToken);
            if (page is null)
            {
                continue;
            }

            var documentType = slug == "terms" ? LegalDocumentType.Terms : LegalDocumentType.Privacy;
            await EnsureLegalVersionAsync(
                documentType,
                "ar",
                page.TitleAr,
                page.ContentAr,
                page.PublishedAtUtc ?? DateTimeOffset.UtcNow,
                cancellationToken);
            await EnsureLegalVersionAsync(
                documentType,
                "en",
                page.TitleEn,
                page.ContentEn,
                page.PublishedAtUtc ?? DateTimeOffset.UtcNow,
                cancellationToken);
        }
    }

    private async Task EnsureLegalVersionAsync(
        LegalDocumentType documentType,
        string culture,
        string title,
        string content,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.LegalDocumentVersions
            .AnyAsync(
                version => version.DocumentType == documentType && version.Culture == culture,
                cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.LegalDocumentVersions.Add(new LegalDocumentVersion(
            documentType,
            culture,
            versionNumber: 1,
            title,
            content,
            publishedAtUtc,
            isCurrentMandatory: true));
    }

    private async Task SeedSystemPagesAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in SystemPageDefinitions)
        {
            var existing = await dbContext.CmsPages
                .FirstOrDefaultAsync(page => page.Slug == definition.Slug, cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            var page = new CmsPage(
                definition.Slug,
                definition.TitleAr,
                definition.TitleEn,
                definition.ContentAr,
                definition.ContentEn,
                definition.MetaTitleAr,
                definition.MetaTitleEn,
                definition.MetaDescriptionAr,
                definition.MetaDescriptionEn,
                isSystemPage: true);
            page.Publish();
            dbContext.CmsPages.Add(page);
        }
    }

    private async Task SeedHomepageAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.HomepageContents.AnyAsync(cancellationToken))
        {
            return;
        }

        var homepage = new HomepageContent(
            "اعثر على المدرسة المناسبة لطفلك",
            "Find the right school for your child",
            "منصة سكوليرا تجمع المدارس والأهالي في مكان واحد للبحث والتقديم والمتابعة.",
            "Schoolera brings schools and parents together to search, apply, and follow up in one place.",
            "تصفح المدارس",
            "Browse schools",
            "/schools",
            "كيف تعمل المنصة",
            "How it works",
            "/how-it-works",
            "مدارس مميزة",
            "Featured schools",
            "رحلة ولي الأمر",
            "Parent journey",
            "<p>ابحث عن المدارس، قارن الخيارات، وقدّم طلبات القبول لمستقبل طفلك.</p>",
            "<p>Search schools, compare options, and submit admission applications for your child.</p>",
            "رحلة المدرسة",
            "School journey",
            "<p>أنشئ ملف مدرستك، اعرض برامجك، واستقبل طلبات القبول عبر سكوليرا.</p>",
            "<p>Create your school profile, showcase programs, and receive admission applications through Schoolera.</p>",
            "الأسئلة الشائعة",
            "Frequently asked questions",
            "إجابات سريعة عن المنصة",
            "Quick answers about the platform");
        homepage.Publish();
        dbContext.HomepageContents.Add(homepage);
    }

    private async Task SeedFaqAsync(CancellationToken cancellationToken)
    {
        await EnsureInterviewFaqCategoryAsync(cancellationToken);

        if (await dbContext.FaqCategories.AnyAsync(
                category => category.Slug != CmsSlugs.InterviewFaqCategorySlug,
                cancellationToken))
        {
            return;
        }

        var categories = new (string Slug, string NameAr, string NameEn, (string QAr, string QEn, string AAr, string AEn)[] Items)[]
        {
            (
                "parents",
                "لأولياء الأمور",
                "For parents",
                new[]
                {
                    (
                        "هل التسجيل مجاني؟",
                        "Is registration free?",
                        "<p>نعم، إنشاء حساب ولي أمر وتصفح المدارس مجاني على سكوليرا.</p>",
                        "<p>Yes, creating a parent account and browsing schools is free on Schoolera.</p>"),
                    (
                        "كيف أقدّم طلب قبول؟",
                        "How do I submit an admission application?",
                        "<p>بعد تسجيل الدخول، اختر المدرسة ثم ابدأ طلب القبول من ملف المدرسة.</p>",
                        "<p>After signing in, choose a school and start an admission application from the school profile.</p>"),
                }),
            (
                "schools",
                "للمدارس",
                "For schools",
                new[]
                {
                    (
                        "كيف تنضم مدرستي؟",
                        "How does my school join?",
                        "<p>يمكن لمالك المدرسة تقديم طلب انضمام عبر صفحة الانضمام، ثم مراجعة فريق سكوليرا.</p>",
                        "<p>A school owner can submit an onboarding request, then the Schoolera team reviews it.</p>"),
                    (
                        "هل يمكن إدارة الفروع؟",
                        "Can we manage branches?",
                        "<p>نعم، يمكن إدارة الفروع والعروض والرسوم من بوابة المدرسة بعد الموافقة.</p>",
                        "<p>Yes, branches, offerings, and fees can be managed from the school portal after approval.</p>"),
                }),
            (
                "platform",
                "عن المنصة",
                "About the platform",
                new[]
                {
                    (
                        "ما هي سكوليرا؟",
                        "What is Schoolera?",
                        "<p>سكوليرا منصة تربط الأهالي بالمدارس لاكتشاف الخيارات التعليمية وإدارة طلبات القبول.</p>",
                        "<p>Schoolera connects parents and schools to discover education options and manage admission applications.</p>"),
                }),
        };

        var sortOrder = 1;
        foreach (var categoryDefinition in categories)
        {
            var category = new FaqCategory(
                categoryDefinition.NameAr,
                categoryDefinition.NameEn,
                categoryDefinition.Slug,
                sortOrder++);
            category.Publish();
            dbContext.FaqCategories.Add(category);

            var itemOrder = 1;
            foreach (var itemDefinition in categoryDefinition.Items)
            {
                var item = new FaqItem(
                    category.Id,
                    itemDefinition.QAr,
                    itemDefinition.QEn,
                    itemDefinition.AAr,
                    itemDefinition.AEn,
                    itemOrder++);
                item.Publish();
                dbContext.FaqItems.Add(item);
            }
        }
    }

    private async Task EnsureInterviewFaqCategoryAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.FaqCategories.AnyAsync(
            category => category.Slug == CmsSlugs.InterviewFaqCategorySlug,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var maxSort = await dbContext.FaqCategories.MaxAsync(
            category => (int?)category.SortOrder,
            cancellationToken);
        var category = new FaqCategory(
            "أسئلة المقابلة والتقييم",
            "Interview and assessment FAQs",
            CmsSlugs.InterviewFaqCategorySlug,
            (maxSort ?? 0) + 1);
        category.Publish();
        dbContext.FaqCategories.Add(category);
    }

    private async Task SeedDemoContactRequestAsync(CancellationToken cancellationToken)
    {
        // Legacy single New row (kept for older DBs that already have it).
        if (!await dbContext.ContactRequests.AnyAsync(cancellationToken))
        {
            var legacy = new ContactRequest(
                "CNT-2026-0001",
                "أحمد مثال",
                "+201000000001",
                "demo-contact@example.invalid",
                ContactCategories.General,
                "استفسار تجريبي عن المنصة",
                "هذا طلب تواصل تجريبي لأغراض العرض فقط.",
                ContactSources.ContactPage);
            dbContext.ContactRequests.Add(legacy);
        }

        await EnsureDemoContactAsync(
            "CNT-DEMO-NEW",
            "سارة تجريبية",
            "+201000000010",
            "demo-new@example.invalid",
            "استفسار جديد — تجريبي",
            "طلب تواصل تجريبي بحالة جديد.",
            ContactRequestStatus.New,
            actorUserId: null,
            cancellationToken);

        await EnsureDemoContactAsync(
            "CNT-DEMO-INREVIEW",
            "محمود تجريبي",
            "+201000000011",
            "demo-inreview@example.invalid",
            "استفسار قيد المراجعة — تجريبي",
            "طلب تواصل تجريبي بحالة قيد المراجعة.",
            ContactRequestStatus.InReview,
            actorUserId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            cancellationToken);

        await EnsureDemoContactAsync(
            "CNT-DEMO-RESOLVED",
            "ليلى تجريبية",
            "+201000000012",
            "demo-resolved@example.invalid",
            "استفسار محلول — تجريبي",
            "طلب تواصل تجريبي بحالة محلول.",
            ContactRequestStatus.Resolved,
            actorUserId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            cancellationToken);
    }

    private async Task EnsureDemoContactAsync(
        string reference,
        string name,
        string phone,
        string email,
        string subject,
        string message,
        ContactRequestStatus targetStatus,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ContactRequests.AsNoTracking()
                .AnyAsync(request => request.Reference == reference, cancellationToken))
        {
            return;
        }

        var request = new ContactRequest(
            reference,
            name,
            phone,
            email,
            ContactCategories.General,
            subject,
            message,
            ContactSources.ContactPage);

        var actor = actorUserId ?? Guid.Empty;
        if (targetStatus == ContactRequestStatus.InReview)
        {
            request.StartReview(actor, "Seed InReview");
        }
        else if (targetStatus == ContactRequestStatus.Resolved)
        {
            request.Resolve(actor, "Seed Resolved");
        }

        dbContext.ContactRequests.Add(request);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("CMS demo contact seeded: {Reference} ({Status}).", reference, targetStatus);
        }
        catch (DbUpdateException)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries<ContactRequest>().ToList())
            {
                if (entry.Entity == request)
                {
                    entry.State = EntityState.Detached;
                }
            }

            if (await dbContext.ContactRequests.AsNoTracking()
                    .AnyAsync(item => item.Reference == reference, cancellationToken))
            {
                logger.LogInformation(
                    "CMS demo contact {Reference} already inserted by concurrent seed.",
                    reference);
                return;
            }

            throw;
        }
    }

    private static readonly SystemPageDefinition[] SystemPageDefinitions =
    [
        new(
            "about",
            "عن سكوليرا",
            "About Schoolera",
            "<h2>من نحن</h2><p>سكوليرا منصة تعليمية تساعد الأهالي على اكتشاف المدارس وإدارة رحلة القبول.</p><ul><li>بحث موحّد عن المدارس</li><li>طلبات قبول رقمية</li><li>متابعة واضحة للحالة</li></ul>",
            "<h2>Who we are</h2><p>Schoolera is an education platform that helps parents discover schools and manage the admission journey.</p><ul><li>Unified school search</li><li>Digital admission applications</li><li>Clear status tracking</li></ul>",
            "عن سكوليرا",
            "About Schoolera",
            "تعرف على رؤية ومهمة منصة سكوليرا.",
            "Learn about Schoolera's vision and mission."),
        new(
            "how-it-works",
            "كيف تعمل المنصة",
            "How it works",
            "<h2>لأولياء الأمور</h2><ul><li>أنشئ حساب ولي أمر</li><li>ابحث عن المدارس</li><li>قدّم طلب القبول</li></ul><h2>للمدارس</h2><ul><li>قدّم طلب انضمام</li><li>أكمل ملف المدرسة</li><li>استقبل الطلبات</li></ul>",
            "<h2>For parents</h2><ul><li>Create a parent account</li><li>Search for schools</li><li>Submit an application</li></ul><h2>For schools</h2><ul><li>Submit onboarding</li><li>Complete the school profile</li><li>Receive applications</li></ul>",
            "كيف تعمل سكوليرا",
            "How Schoolera works",
            "خطوات استخدام المنصة للأهالي والمدارس.",
            "Steps to use the platform for parents and schools."),
        new(
            "privacy",
            "سياسة الخصوصية",
            "Privacy policy",
            "<h2>خصوصيتك مهمة</h2><p>نحمي بياناتك الشخصية ونستخدمها فقط لتقديم خدمات المنصة وتحسينها.</p><ul><li>لا نبيع بياناتك</li><li>نطبق ضوابط وصول صارمة</li><li>يمكنك طلب تحديث بياناتك</li></ul>",
            "<h2>Your privacy matters</h2><p>We protect your personal data and use it only to provide and improve platform services.</p><ul><li>We do not sell your data</li><li>We apply strict access controls</li><li>You may request data updates</li></ul>",
            "سياسة الخصوصية",
            "Privacy policy",
            "كيف نتعامل مع بياناتك على سكوليرا.",
            "How we handle your data on Schoolera."),
        new(
            "terms",
            "الشروط والأحكام",
            "Terms of service",
            "<h2>استخدام المنصة</h2><p>باستخدام سكوليرا فإنك توافق على الالتزام بهذه الشروط واستخدام المنصة بشكل قانوني ومسؤول.</p><ul><li>معلومات دقيقة عند التسجيل</li><li>احترام خصوصية الآخرين</li><li>عدم إساءة استخدام الخدمة</li></ul>",
            "<h2>Using the platform</h2><p>By using Schoolera you agree to these terms and to use the platform lawfully and responsibly.</p><ul><li>Provide accurate registration information</li><li>Respect others' privacy</li><li>Do not misuse the service</li></ul>",
            "الشروط والأحكام",
            "Terms of service",
            "شروط استخدام منصة سكوليرا.",
            "Terms for using the Schoolera platform."),
        new(
            "sla",
            "اتفاقية مستوى الخدمة",
            "Service level agreement",
            "<h2>التزامنا</h2><p>نسعى لتوفير منصة متاحة وآمنة مع دعم فني خلال ساعات العمل الرسمية.</p><ul><li>مراقبة استقرار الخدمة</li><li>معالجة الأعطال الحرجة بأولوية</li><li>تحديثات أمنية دورية</li></ul>",
            "<h2>Our commitment</h2><p>We aim to provide a secure, available platform with support during official business hours.</p><ul><li>Service stability monitoring</li><li>Priority handling of critical incidents</li><li>Regular security updates</li></ul>",
            "اتفاقية مستوى الخدمة",
            "Service level agreement",
            "مستوى الخدمة والدعم على سكوليرا.",
            "Service and support levels on Schoolera."),
        // Optional intro overlay for the Angular /contact form page (route is not CmsStaticPage).
        new(
            "contact",
            "تواصل معنا",
            "Contact us",
            "<p>يسعدنا استقبال استفساراتكم حول المنصة أو المدارس أو الدعم الفني. أرسل رسالتك وسنرد في أقرب وقت ممكن.</p>",
            "<p>We welcome questions about the platform, schools, or technical support. Send your message and we will get back to you as soon as possible.</p>",
            "تواصل معنا | سكوليرا",
            "Contact us | Schoolera",
            "تواصل مع فريق سكوليرا للدعم والاستفسارات.",
            "Contact the Schoolera team for support and inquiries."),
    ];

    private sealed record SystemPageDefinition(
        string Slug,
        string TitleAr,
        string TitleEn,
        string ContentAr,
        string ContentEn,
        string MetaTitleAr,
        string MetaTitleEn,
        string MetaDescriptionAr,
        string MetaDescriptionEn);
}
