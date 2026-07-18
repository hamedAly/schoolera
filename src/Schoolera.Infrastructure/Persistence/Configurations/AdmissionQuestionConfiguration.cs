using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolAdmissionQuestionConfiguration : IEntityTypeConfiguration<SchoolAdmissionQuestion>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionQuestion> builder)
    {
        builder.ToTable("SchoolAdmissionQuestions");
        builder.HasKey(question => question.Id);

        builder.Property(question => question.QuestionCode)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionCode)
            .IsRequired();
        builder.Property(question => question.QuestionType).HasConversion<int>().IsRequired();
        builder.Property(question => question.LabelAr)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionLabel)
            .IsRequired();
        builder.Property(question => question.LabelEn)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionLabel)
            .IsRequired();
        builder.Property(question => question.HelpAr)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionHelp);
        builder.Property(question => question.HelpEn)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionHelp);
        builder.Property(question => question.PublicationStatus).HasConversion<int>().IsRequired();
        builder.Property(question => question.ScopeKey)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementScopeKey)
            .IsRequired();
        builder.Property(question => question.AllowedFileExtensions)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionAllowedFiles);

        builder.HasIndex(question => new { question.SchoolId, question.QuestionCode, question.ScopeKey })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [PublicationStatus] = 2");

        builder.HasIndex(question => new { question.SchoolId, question.SortOrder });

        builder.HasOne(question => question.School)
            .WithMany()
            .HasForeignKey(question => question.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(question => question.Options)
            .WithOne(option => option.Question)
            .HasForeignKey(option => option.SchoolAdmissionQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SchoolAdmissionQuestionOptionConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionQuestionOption>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionQuestionOption> builder)
    {
        builder.ToTable("SchoolAdmissionQuestionOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.OptionCode)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionOptionCode)
            .IsRequired();
        builder.Property(option => option.LabelAr)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionOptionLabel)
            .IsRequired();
        builder.Property(option => option.LabelEn)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionOptionLabel)
            .IsRequired();
        builder.HasIndex(option => new { option.SchoolAdmissionQuestionId, option.OptionCode })
            .IsUnique();
    }
}

public sealed class SchoolAdmissionQuestionAuditConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionQuestionAudit>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionQuestionAudit> builder)
    {
        builder.ToTable("SchoolAdmissionQuestionAudits");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Action)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionAuditAction)
            .IsRequired();
        builder.Property(audit => audit.Metadata)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionAuditMetadata);
        builder.HasIndex(audit => new { audit.SchoolId, audit.CreatedAtUtc });
    }
}

public sealed class AdmissionApplicationQuestionSnapshotConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationQuestionSnapshot>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationQuestionSnapshot> builder)
    {
        builder.ToTable("AdmissionApplicationQuestionSnapshots");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.QuestionCode)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionCode)
            .IsRequired();
        builder.Property(snapshot => snapshot.QuestionType).HasConversion<int>().IsRequired();
        builder.Property(snapshot => snapshot.LabelAr)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionLabel)
            .IsRequired();
        builder.Property(snapshot => snapshot.LabelEn)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionLabel)
            .IsRequired();
        builder.Property(snapshot => snapshot.HelpAr)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionHelp);
        builder.Property(snapshot => snapshot.HelpEn)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionHelp);
        builder.Property(snapshot => snapshot.AllowedFileExtensions)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionAllowedFiles);

        builder.HasIndex(snapshot => new { snapshot.AdmissionApplicationId, snapshot.QuestionCode })
            .IsUnique();
        builder.HasIndex(snapshot => new { snapshot.AdmissionApplicationId, snapshot.SortOrder });

        builder.HasOne(snapshot => snapshot.AdmissionApplication)
            .WithMany(application => application.QuestionSnapshots)
            .HasForeignKey(snapshot => snapshot.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(snapshot => snapshot.Options)
            .WithOne(option => option.QuestionSnapshot)
            .HasForeignKey(option => option.QuestionSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AdmissionApplicationQuestionSnapshotOptionConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationQuestionSnapshotOption>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationQuestionSnapshotOption> builder)
    {
        builder.ToTable("AdmissionApplicationQuestionSnapshotOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.OptionCode)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionOptionCode)
            .IsRequired();
        builder.Property(option => option.LabelAr)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionOptionLabel)
            .IsRequired();
        builder.Property(option => option.LabelEn)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionOptionLabel)
            .IsRequired();
        builder.HasIndex(option => new { option.QuestionSnapshotId, option.OptionCode }).IsUnique();
    }
}

public sealed class AdmissionApplicationAnswerConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationAnswer>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationAnswer> builder)
    {
        builder.ToTable("AdmissionApplicationAnswers");
        builder.HasKey(answer => answer.Id);
        builder.Property(answer => answer.TextValue)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionAnswerText);
        builder.Property(answer => answer.SelectedOptionCodes)
            .HasMaxLength(FieldLengthLimits.AdmissionQuestionSelectedOptions);

        builder.HasIndex(answer => new { answer.AdmissionApplicationId, answer.QuestionSnapshotId })
            .IsUnique();

        builder.HasOne(answer => answer.AdmissionApplication)
            .WithMany(application => application.Answers)
            .HasForeignKey(answer => answer.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(answer => answer.QuestionSnapshot)
            .WithOne(snapshot => snapshot.Answer)
            .HasForeignKey<AdmissionApplicationAnswer>(answer => answer.QuestionSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(answer => answer.Attachment)
            .WithMany()
            .HasForeignKey(answer => answer.AttachmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
