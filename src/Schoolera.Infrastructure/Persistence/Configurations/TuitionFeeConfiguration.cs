using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class TuitionFeeConfiguration : IEntityTypeConfiguration<TuitionFee>
{
    public void Configure(EntityTypeBuilder<TuitionFee> builder)
    {
        builder.ToTable("TuitionFees");

        builder.HasKey(fee => fee.Id);

        builder.Property(fee => fee.Category)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(FeeCategory.Tuition);

        builder.Property(fee => fee.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(fee => fee.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(fee => fee.CurrencyCode)
            .HasMaxLength(FieldLengthLimits.CurrencyCode)
            .IsRequired();

        builder.Property(fee => fee.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(fee => fee.IsStartingFrom)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(fee => fee.NotesAr)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.Property(fee => fee.NotesEn)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.Property(fee => fee.InternalNotesAr)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.Property(fee => fee.InternalNotesEn)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.Property(fee => fee.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(fee => fee.IsPublished)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(fee => fee.CreatedAtUtc)
            .IsRequired();

        builder.Property(fee => fee.UpdatedAtUtc)
            .IsRequired();

        builder.HasMany(fee => fee.Installments)
            .WithOne(installment => installment.TuitionFee)
            .HasForeignKey(installment => installment.TuitionFeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // One active fee per branch/stage/grade/year/category.
        builder.HasIndex(fee => new
            {
                fee.SchoolBranchId,
                fee.EducationalStageId,
                fee.GradeId,
                fee.AcademicYearId,
                fee.Category,
            })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne(fee => fee.EducationalStage)
            .WithMany()
            .HasForeignKey(fee => fee.EducationalStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fee => fee.Grade)
            .WithMany()
            .HasForeignKey(fee => fee.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fee => fee.AcademicYear)
            .WithMany()
            .HasForeignKey(fee => fee.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SchoolFeeInstallmentDisplayConfiguration
    : IEntityTypeConfiguration<SchoolFeeInstallmentDisplay>
{
    public void Configure(EntityTypeBuilder<SchoolFeeInstallmentDisplay> builder)
    {
        builder.ToTable("SchoolFeeInstallmentDisplays");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.NameAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(item => item.NameEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(item => item.AmountMode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(item => item.FixedAmount)
            .HasPrecision(18, 2);

        builder.Property(item => item.Percentage)
            .HasPrecision(9, 4);

        builder.Property(item => item.NotesAr)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.Property(item => item.NotesEn)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.HasIndex(item => new { item.TuitionFeeId, item.SequenceNumber });
        builder.HasIndex(item => item.TuitionFeeId);
    }
}

public sealed class SchoolPublishedDiscountConfiguration
    : IEntityTypeConfiguration<SchoolPublishedDiscount>
{
    public void Configure(EntityTypeBuilder<SchoolPublishedDiscount> builder)
    {
        builder.ToTable("SchoolPublishedDiscounts");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.TitleAr)
            .HasMaxLength(FieldLengthLimits.TaxonomyName)
            .IsRequired();

        builder.Property(item => item.TitleEn)
            .HasMaxLength(FieldLengthLimits.TaxonomyName);

        builder.Property(item => item.EligibilityDescriptionAr)
            .HasMaxLength(FieldLengthLimits.Notes)
            .IsRequired();

        builder.Property(item => item.EligibilityDescriptionEn)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.Property(item => item.DiscountType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(item => item.Value)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.CurrencyCode)
            .HasMaxLength(FieldLengthLimits.CurrencyCode);

        builder.HasOne(item => item.School)
            .WithMany(school => school.PublishedDiscounts)
            .HasForeignKey(item => item.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.SchoolBranch)
            .WithMany()
            .HasForeignKey(item => item.SchoolBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.EducationalStage)
            .WithMany()
            .HasForeignKey(item => item.EducationalStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.Grade)
            .WithMany()
            .HasForeignKey(item => item.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.AcademicYear)
            .WithMany()
            .HasForeignKey(item => item.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.SchoolId, item.IsActive, item.IsPublished });
    }
}

public sealed class SchoolFinancialNoteConfiguration : IEntityTypeConfiguration<SchoolFinancialNote>
{
    public void Configure(EntityTypeBuilder<SchoolFinancialNote> builder)
    {
        builder.ToTable("SchoolFinancialNotes");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.TextAr)
            .HasMaxLength(FieldLengthLimits.Notes)
            .IsRequired();

        builder.Property(item => item.TextEn)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.HasOne(item => item.School)
            .WithMany(school => school.FinancialNotes)
            .HasForeignKey(item => item.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new { item.SchoolId, item.IsInternal, item.IsActive });
    }
}
