using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolContactLeadConfiguration : IEntityTypeConfiguration<SchoolContactLead>
{
    public void Configure(EntityTypeBuilder<SchoolContactLead> builder)
    {
        builder.ToTable("SchoolContactLeads");
        builder.HasKey(lead => lead.Id);

        builder.Property(lead => lead.Source).HasMaxLength(64).IsRequired();
        builder.Property(lead => lead.Name).HasMaxLength(FieldLengthLimits.PersonName).IsRequired();
        builder.Property(lead => lead.Phone).HasMaxLength(FieldLengthLimits.Phone).IsRequired();
        builder.Property(lead => lead.Email).HasMaxLength(FieldLengthLimits.Email);
        builder.Property(lead => lead.Message).HasMaxLength(FieldLengthLimits.Notes);
        builder.Property(lead => lead.Culture).HasMaxLength(FieldLengthLimits.PreferredLanguage).IsRequired();

        builder.HasIndex(lead => new { lead.SchoolId, lead.CreatedAtUtc });

        builder.HasOne(lead => lead.School)
            .WithMany()
            .HasForeignKey(lead => lead.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SchoolProfileViewDailyConfiguration : IEntityTypeConfiguration<SchoolProfileViewDaily>
{
    public void Configure(EntityTypeBuilder<SchoolProfileViewDaily> builder)
    {
        builder.ToTable("SchoolProfileViewDaily");
        builder.HasKey(row => row.Id);

        builder.HasIndex(row => new { row.SchoolId, row.ViewDateUtc }).IsUnique();

        builder.HasOne(row => row.School)
            .WithMany()
            .HasForeignKey(row => row.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
