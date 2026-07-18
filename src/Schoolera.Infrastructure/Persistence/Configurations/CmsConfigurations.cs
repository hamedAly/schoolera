using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class CmsPageConfiguration : IEntityTypeConfiguration<CmsPage>
{
    public void Configure(EntityTypeBuilder<CmsPage> builder)
    {
        builder.ToTable("CmsPages");
        builder.HasKey(page => page.Id);

        builder.Property(page => page.Slug).HasMaxLength(FieldLengthLimits.Slug).IsRequired();
        builder.Property(page => page.TitleAr).HasMaxLength(FieldLengthLimits.CmsTitle).IsRequired();
        builder.Property(page => page.TitleEn).HasMaxLength(FieldLengthLimits.CmsTitle).IsRequired();
        builder.Property(page => page.ContentAr).HasMaxLength(FieldLengthLimits.CmsContent).IsRequired();
        builder.Property(page => page.ContentEn).HasMaxLength(FieldLengthLimits.CmsContent).IsRequired();
        builder.Property(page => page.MetaTitleAr).HasMaxLength(FieldLengthLimits.CmsMetaTitle);
        builder.Property(page => page.MetaTitleEn).HasMaxLength(FieldLengthLimits.CmsMetaTitle);
        builder.Property(page => page.MetaDescriptionAr).HasMaxLength(FieldLengthLimits.CmsMetaDescription);
        builder.Property(page => page.MetaDescriptionEn).HasMaxLength(FieldLengthLimits.CmsMetaDescription);
        builder.Property(page => page.RowVersion).IsRowVersion();

        builder.HasIndex(page => page.Slug).IsUnique();
        builder.HasIndex(page => page.Status);
        builder.HasIndex(page => page.PublishedAtUtc);
    }
}

public sealed class FaqCategoryConfiguration : IEntityTypeConfiguration<FaqCategory>
{
    public void Configure(EntityTypeBuilder<FaqCategory> builder)
    {
        builder.ToTable("FaqCategories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.NameAr).HasMaxLength(FieldLengthLimits.CmsTitle).IsRequired();
        builder.Property(category => category.NameEn).HasMaxLength(FieldLengthLimits.CmsTitle).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(FieldLengthLimits.Slug).IsRequired();

        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasIndex(category => category.SortOrder);
        builder.HasIndex(category => category.IsPublished);
    }
}

public sealed class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> builder)
    {
        builder.ToTable("FaqItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.QuestionAr).HasMaxLength(FieldLengthLimits.FaqQuestion).IsRequired();
        builder.Property(item => item.QuestionEn).HasMaxLength(FieldLengthLimits.FaqQuestion).IsRequired();
        builder.Property(item => item.AnswerAr).HasMaxLength(FieldLengthLimits.FaqAnswer).IsRequired();
        builder.Property(item => item.AnswerEn).HasMaxLength(FieldLengthLimits.FaqAnswer).IsRequired();
        builder.Property(item => item.OwnershipScope).HasConversion<int>().HasDefaultValue(FaqOwnershipScope.Platform);
        builder.Property(item => item.IsActive).HasDefaultValue(true);
        builder.Property(item => item.RowVersion).IsRowVersion();

        builder.HasIndex(item => new { item.FaqCategoryId, item.SortOrder });
        builder.HasIndex(item => item.IsPublished);
        builder.HasIndex(item => new { item.OwnershipScope, item.SchoolId, item.IsPublished, item.IsActive });
        builder.HasIndex(item => item.InterviewCategory);
        builder.HasIndex(item => new { item.SchoolId, item.SortOrder });

        builder.HasOne(item => item.FaqCategory)
            .WithMany(category => category.Items)
            .HasForeignKey(item => item.FaqCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.School)
            .WithMany()
            .HasForeignKey(item => item.SchoolId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

public sealed class HomepageContentConfiguration : IEntityTypeConfiguration<HomepageContent>
{
    public void Configure(EntityTypeBuilder<HomepageContent> builder)
    {
        builder.ToTable("HomepageContents");
        builder.HasKey(content => content.Id);

        builder.Property(content => content.HeroTitleAr).HasMaxLength(FieldLengthLimits.HomepageHeroTitle).IsRequired();
        builder.Property(content => content.HeroTitleEn).HasMaxLength(FieldLengthLimits.HomepageHeroTitle).IsRequired();
        builder.Property(content => content.HeroSubtitleAr).HasMaxLength(FieldLengthLimits.HomepageHeroSubtitle).IsRequired();
        builder.Property(content => content.HeroSubtitleEn).HasMaxLength(FieldLengthLimits.HomepageHeroSubtitle).IsRequired();
        builder.Property(content => content.PrimaryCtaLabelAr).HasMaxLength(FieldLengthLimits.HomepageCtaLabel).IsRequired();
        builder.Property(content => content.PrimaryCtaLabelEn).HasMaxLength(FieldLengthLimits.HomepageCtaLabel).IsRequired();
        builder.Property(content => content.PrimaryCtaUrl).HasMaxLength(FieldLengthLimits.Url).IsRequired();
        builder.Property(content => content.SecondaryCtaLabelAr).HasMaxLength(FieldLengthLimits.HomepageCtaLabel);
        builder.Property(content => content.SecondaryCtaLabelEn).HasMaxLength(FieldLengthLimits.HomepageCtaLabel);
        builder.Property(content => content.SecondaryCtaUrl).HasMaxLength(FieldLengthLimits.Url);
        builder.Property(content => content.SchoolsSectionTitleAr).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.SchoolsSectionTitleEn).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.ParentJourneyTitleAr).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.ParentJourneyTitleEn).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.ParentJourneyTextAr).HasMaxLength(FieldLengthLimits.HomepageSectionText).IsRequired();
        builder.Property(content => content.ParentJourneyTextEn).HasMaxLength(FieldLengthLimits.HomepageSectionText).IsRequired();
        builder.Property(content => content.SchoolJourneyTitleAr).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.SchoolJourneyTitleEn).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.SchoolJourneyTextAr).HasMaxLength(FieldLengthLimits.HomepageSectionText).IsRequired();
        builder.Property(content => content.SchoolJourneyTextEn).HasMaxLength(FieldLengthLimits.HomepageSectionText).IsRequired();
        builder.Property(content => content.FaqSectionTitleAr).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.FaqSectionTitleEn).HasMaxLength(FieldLengthLimits.HomepageSectionTitle).IsRequired();
        builder.Property(content => content.FaqSectionSubtitleAr).HasMaxLength(FieldLengthLimits.HomepageHeroSubtitle);
        builder.Property(content => content.FaqSectionSubtitleEn).HasMaxLength(FieldLengthLimits.HomepageHeroSubtitle);
        builder.Property(content => content.RowVersion).IsRowVersion();

        builder.HasIndex(content => content.Status);
        builder.HasIndex(content => content.PublishedAtUtc);
    }
}

public sealed class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
{
    public void Configure(EntityTypeBuilder<ContactRequest> builder)
    {
        builder.ToTable("ContactRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.Reference).HasMaxLength(FieldLengthLimits.ContactReference).IsRequired();
        builder.Property(request => request.Name).HasMaxLength(FieldLengthLimits.PersonName).IsRequired();
        builder.Property(request => request.Phone).HasMaxLength(FieldLengthLimits.Phone).IsRequired();
        builder.Property(request => request.Email).HasMaxLength(FieldLengthLimits.Email);
        builder.Property(request => request.Category).HasMaxLength(FieldLengthLimits.ContactCategory).IsRequired();
        builder.Property(request => request.Subject).HasMaxLength(FieldLengthLimits.ContactSubject).IsRequired();
        builder.Property(request => request.Message).HasMaxLength(FieldLengthLimits.ContactMessage).IsRequired();
        builder.Property(request => request.Source).HasMaxLength(FieldLengthLimits.ContactSource).IsRequired();
        builder.Property(request => request.AdminNote).HasMaxLength(FieldLengthLimits.ContactAdminNote);

        builder.HasIndex(request => request.Reference).IsUnique();
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.Category);
        builder.HasIndex(request => request.CreatedAtUtc);
    }
}
