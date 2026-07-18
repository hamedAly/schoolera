using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class AdmissionApplicationConfiguration : IEntityTypeConfiguration<AdmissionApplication>
{
    public void Configure(EntityTypeBuilder<AdmissionApplication> builder)
    {
        builder.ToTable("AdmissionApplications");
        builder.HasKey(application => application.Id);

        builder.Property(application => application.ApplicationNumber)
            .HasMaxLength(FieldLengthLimits.ApplicationNumber)
            .IsRequired();

        builder.HasIndex(application => application.ApplicationNumber).IsUnique();

        builder.Property(application => application.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(application => application.ParentNotes)
            .HasMaxLength(FieldLengthLimits.AdmissionParentNotes);

        builder.Property(application => application.SchoolNotes)
            .HasMaxLength(FieldLengthLimits.AdmissionSchoolNotes);

        builder.Property(application => application.RejectionReason)
            .HasMaxLength(FieldLengthLimits.AdmissionRejectionReason);

        builder.Property(application => application.CancellationReason)
            .HasMaxLength(FieldLengthLimits.AdmissionCancellationReason);

        builder.Property(application => application.SubmittedChildFullName)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);

        builder.Property(application => application.SubmittedChildCurrentSchoolName)
            .HasMaxLength(FieldLengthLimits.ChildCurrentSchoolName);
        builder.Property(application => application.SubmittedChildSkills)
            .HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(application => application.SubmittedChildHobbies)
            .HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(application => application.SubmittedChildStrengths)
            .HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(application => application.SubmittedChildImprovementAreas)
            .HasMaxLength(FieldLengthLimits.ChildFreeText);
        builder.Property(application => application.SubmittedChildSpecialNeedsNotes)
            .HasMaxLength(FieldLengthLimits.Notes);

        builder.Property(application => application.SubmittedSchoolNameAr)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);
        builder.Property(application => application.SubmittedSchoolNameEn)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);

        builder.Property(application => application.SubmittedBranchNameAr)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);
        builder.Property(application => application.SubmittedBranchNameEn)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);

        builder.Property(application => application.SubmittedStageNameAr)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);
        builder.Property(application => application.SubmittedStageNameEn)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);

        builder.Property(application => application.SubmittedGradeNameAr)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);
        builder.Property(application => application.SubmittedGradeNameEn)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);

        builder.Property(application => application.SubmittedAcademicYearNameAr)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);
        builder.Property(application => application.SubmittedAcademicYearNameEn)
            .HasMaxLength(FieldLengthLimits.AdmissionSnapshotName);

        builder.Property(application => application.SubmittedParentDisplayName)
            .HasMaxLength(FieldLengthLimits.PersonName);
        builder.Property(application => application.SubmittedParentEmail)
            .HasMaxLength(FieldLengthLimits.Email);
        builder.Property(application => application.SubmittedParentPhone)
            .HasMaxLength(FieldLengthLimits.Phone);
        builder.Property(application => application.SubmittedParentAlternatePhone)
            .HasMaxLength(FieldLengthLimits.Phone);

        builder.Property(application => application.SubmittedFatherFullName)
            .HasMaxLength(FieldLengthLimits.PersonName);
        builder.Property(application => application.SubmittedFatherPhone)
            .HasMaxLength(FieldLengthLimits.Phone);
        builder.Property(application => application.SubmittedFatherEmail)
            .HasMaxLength(FieldLengthLimits.Email);
        builder.Property(application => application.SubmittedFatherOccupation)
            .HasMaxLength(FieldLengthLimits.ParentOccupation);
        builder.Property(application => application.SubmittedFatherQualification)
            .HasMaxLength(FieldLengthLimits.ParentQualification);
        builder.Property(application => application.SubmittedFatherMaskedIdentity)
            .HasMaxLength(FieldLengthLimits.GuardianMaskedIdentity);

        builder.Property(application => application.SubmittedMotherFullName)
            .HasMaxLength(FieldLengthLimits.PersonName);
        builder.Property(application => application.SubmittedMotherPhone)
            .HasMaxLength(FieldLengthLimits.Phone);
        builder.Property(application => application.SubmittedMotherEmail)
            .HasMaxLength(FieldLengthLimits.Email);
        builder.Property(application => application.SubmittedMotherOccupation)
            .HasMaxLength(FieldLengthLimits.ParentOccupation);
        builder.Property(application => application.SubmittedMotherQualification)
            .HasMaxLength(FieldLengthLimits.ParentQualification);
        builder.Property(application => application.SubmittedMotherMaskedIdentity)
            .HasMaxLength(FieldLengthLimits.GuardianMaskedIdentity);

        builder.Property(application => application.CreatedAtUtc).IsRequired();
        builder.Property(application => application.UpdatedAtUtc).IsRequired();

        builder.Property(application => application.RowVersion).IsRowVersion();

        builder.HasIndex(application => application.ParentUserId);
        builder.HasIndex(application => application.ChildProfileId);
        builder.HasIndex(application => new { application.SchoolId, application.SchoolBranchId });
        builder.HasIndex(application => new { application.GradeId, application.AcademicYearId });
        builder.HasIndex(application => application.Status);
        builder.HasIndex(application => application.CreatedAtUtc);
        builder.HasIndex(application => application.SubmittedAtUtc);

        // Active duplicate statuses: Draft, Submitted, UnderReview, Accepted,
        // MissingDocuments, InterviewRequired, AssessmentRequired, WaitingList, Registered.
        builder.HasIndex(application => new
            {
                application.ChildProfileId,
                application.SchoolId,
                application.SchoolBranchId,
                application.GradeId,
                application.AcademicYearId,
            })
            .IsUnique()
            .HasFilter("[Status] IN (1, 2, 3, 4, 7, 8, 9, 10, 11)")
            .HasDatabaseName("IX_AdmissionApplications_ActiveDuplicate");

        builder.HasOne(application => application.ParentProfile)
            .WithMany()
            .HasForeignKey(application => application.ParentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.ChildProfile)
            .WithMany()
            .HasForeignKey(application => application.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.School)
            .WithMany()
            .HasForeignKey(application => application.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.SchoolBranch)
            .WithMany()
            .HasForeignKey(application => application.SchoolBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.EducationalStage)
            .WithMany()
            .HasForeignKey(application => application.EducationalStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.Grade)
            .WithMany()
            .HasForeignKey(application => application.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.AcademicYear)
            .WithMany()
            .HasForeignKey(application => application.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(application => application.WaitingListReason)
            .HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);

        builder.HasMany(application => application.Attachments)
            .WithOne(attachment => attachment.AdmissionApplication)
            .HasForeignKey(attachment => attachment.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(application => application.History)
            .WithOne(history => history.AdmissionApplication)
            .HasForeignKey(history => history.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(application => application.RequirementSnapshots)
            .WithOne(snapshot => snapshot.AdmissionApplication)
            .HasForeignKey(snapshot => snapshot.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(application => application.MissingItemsRequests)
            .WithOne()
            .HasForeignKey(request => request.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(application => application.InterviewAppointments)
            .WithOne()
            .HasForeignKey(appointment => appointment.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(application => application.AssessmentAppointments)
            .WithOne()
            .HasForeignKey(appointment => appointment.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.PolicySnapshot)
            .WithOne(snapshot => snapshot.AdmissionApplication)
            .HasForeignKey<AdmissionApplicationInterviewAssessmentPolicySnapshot>(
                snapshot => snapshot.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.AgeEligibilitySnapshot)
            .WithOne(snapshot => snapshot.AdmissionApplication)
            .HasForeignKey<AdmissionApplicationChildAgeEligibilitySnapshot>(
                snapshot => snapshot.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionApplicationAttachmentConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationAttachment>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationAttachment> builder)
    {
        builder.ToTable("AdmissionApplicationAttachments");
        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.AttachmentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(attachment => attachment.OriginalFileName)
            .HasMaxLength(FieldLengthLimits.OnboardingFileName)
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(FieldLengthLimits.OnboardingContentType)
            .IsRequired();

        builder.Property(attachment => attachment.StorageKey)
            .HasMaxLength(FieldLengthLimits.OnboardingStoredReference)
            .IsRequired();

        builder.Property(attachment => attachment.FileSizeBytes).IsRequired();
        builder.Property(attachment => attachment.UploadedByUserId).IsRequired();
        builder.Property(attachment => attachment.CreatedAtUtc).IsRequired();
        builder.Property(attachment => attachment.UpdatedAtUtc).IsRequired();
        builder.Property(attachment => attachment.RequiredDocumentCode).HasConversion<int>();

        builder.HasIndex(attachment => attachment.AdmissionApplicationId);
        builder.HasIndex(attachment => new { attachment.AdmissionApplicationId, attachment.SourceVaultDocumentId });
        builder.HasIndex(attachment => new { attachment.AdmissionApplicationId, attachment.RequirementSnapshotId });
        builder.HasIndex(attachment => new { attachment.AdmissionApplicationId, attachment.QuestionSnapshotId });

        builder.HasOne(attachment => attachment.RequirementSnapshot)
            .WithMany(snapshot => snapshot.Attachments)
            .HasForeignKey(attachment => attachment.RequirementSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(attachment => attachment.QuestionSnapshot)
            .WithMany(snapshot => snapshot.Attachments)
            .HasForeignKey(attachment => attachment.QuestionSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionApplicationHistoryConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationHistory>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationHistory> builder)
    {
        builder.ToTable("AdmissionApplicationHistory");
        builder.HasKey(history => history.Id);

        builder.Property(history => history.FromStatus).HasConversion<int>();
        builder.Property(history => history.ToStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(history => history.Action)
            .HasMaxLength(FieldLengthLimits.AdmissionHistoryAction)
            .IsRequired();

        builder.Property(history => history.ActorRole)
            .HasMaxLength(FieldLengthLimits.AdmissionActorRole)
            .IsRequired();

        builder.Property(history => history.ParentVisibleNote)
            .HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);

        builder.Property(history => history.InternalNote)
            .HasMaxLength(FieldLengthLimits.AdmissionHistoryNote);

        builder.Property(history => history.ActorUserId).IsRequired();
        builder.Property(history => history.ParentVisible).IsRequired();
        builder.Property(history => history.CreatedAtUtc).IsRequired();

        builder.HasIndex(history => new { history.AdmissionApplicationId, history.CreatedAtUtc });
    }
}

public sealed class AdmissionApplicationNumberSequenceConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationNumberSequence>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationNumberSequence> builder)
    {
        builder.ToTable("AdmissionApplicationNumberSequences");
        builder.HasKey(sequence => sequence.Year);
        builder.Property(sequence => sequence.Year).ValueGeneratedNever();
        builder.Property(sequence => sequence.LastValue).IsRequired();
    }
}

public sealed class AdmissionApplicationRequirementSnapshotConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationRequirementSnapshot>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationRequirementSnapshot> builder)
    {
        builder.ToTable("AdmissionApplicationRequirementSnapshots");
        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.RequirementCode)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementCode)
            .IsRequired();
        builder.Property(snapshot => snapshot.Kind).HasConversion<int>().IsRequired();
        builder.Property(snapshot => snapshot.NameAr)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementName)
            .IsRequired();
        builder.Property(snapshot => snapshot.NameEn)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementName)
            .IsRequired();
        builder.Property(snapshot => snapshot.DescriptionAr)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementDescription);
        builder.Property(snapshot => snapshot.DescriptionEn)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementDescription);
        builder.Property(snapshot => snapshot.AllowedFileExtensions)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementAllowedFiles);
        builder.Property(snapshot => snapshot.ProfileFieldCode).HasConversion<int>();
        builder.Property(snapshot => snapshot.DocumentCode).HasConversion<int>();
        builder.Property(snapshot => snapshot.CreatedAtUtc).IsRequired();

        builder.HasIndex(snapshot => new { snapshot.AdmissionApplicationId, snapshot.RequirementCode })
            .IsUnique();
        builder.HasIndex(snapshot => snapshot.AdmissionApplicationId);
    }
}

public sealed class SchoolAdmissionRequirementConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionRequirement>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionRequirement> builder)
    {
        builder.ToTable("SchoolAdmissionRequirements");
        builder.HasKey(requirement => requirement.Id);

        builder.Property(requirement => requirement.RequirementCode)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementCode)
            .IsRequired();
        builder.Property(requirement => requirement.Kind).HasConversion<int>().IsRequired();
        builder.Property(requirement => requirement.NameAr)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementName)
            .IsRequired();
        builder.Property(requirement => requirement.NameEn)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementName)
            .IsRequired();
        builder.Property(requirement => requirement.DescriptionAr)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementDescription);
        builder.Property(requirement => requirement.DescriptionEn)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementDescription);
        builder.Property(requirement => requirement.PublicationStatus).HasConversion<int>().IsRequired();
        builder.Property(requirement => requirement.ScopeKey)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementScopeKey)
            .IsRequired();
        builder.Property(requirement => requirement.AllowedFileExtensions)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementAllowedFiles);
        builder.Property(requirement => requirement.ProfileFieldCode).HasConversion<int>();
        builder.Property(requirement => requirement.DocumentCode).HasConversion<int>();
        builder.Property(requirement => requirement.CreatedByUserId).IsRequired();
        builder.Property(requirement => requirement.UpdatedByUserId).IsRequired();
        builder.Property(requirement => requirement.CreatedAtUtc).IsRequired();
        builder.Property(requirement => requirement.UpdatedAtUtc).IsRequired();

        builder.HasOne(requirement => requirement.School)
            .WithMany()
            .HasForeignKey(requirement => requirement.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent ambiguous published definitions at equal scope for the same code.
        builder.HasIndex(requirement => new
            {
                requirement.SchoolId,
                requirement.RequirementCode,
                requirement.ScopeKey,
            })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [PublicationStatus] = 2");

        builder.HasIndex(requirement => new
            {
                requirement.SchoolId,
                requirement.PublicationStatus,
                requirement.IsActive,
            });
        builder.HasIndex(requirement => new { requirement.SchoolId, requirement.SortOrder });
    }
}

public sealed class SchoolAdmissionRequirementAuditConfiguration
    : IEntityTypeConfiguration<SchoolAdmissionRequirementAudit>
{
    public void Configure(EntityTypeBuilder<SchoolAdmissionRequirementAudit> builder)
    {
        builder.ToTable("SchoolAdmissionRequirementAudits");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Action)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementAuditAction)
            .IsRequired();
        builder.Property(audit => audit.RequirementCode)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementCode);
        builder.Property(audit => audit.Metadata)
            .HasMaxLength(FieldLengthLimits.AdmissionRequirementAuditMetadata);
        builder.Property(audit => audit.ActorUserId).IsRequired();
        builder.Property(audit => audit.CreatedAtUtc).IsRequired();
        builder.HasIndex(audit => new { audit.SchoolId, audit.CreatedAtUtc });
    }
}
