using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTickets");
        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Reference)
            .HasMaxLength(FieldLengthLimits.SupportTicketReference)
            .IsRequired();

        builder.HasIndex(ticket => ticket.Reference).IsUnique();

        builder.Property(ticket => ticket.Subject)
            .HasMaxLength(FieldLengthLimits.SupportTicketSubject)
            .IsRequired();

        builder.Property(ticket => ticket.Category).HasConversion<int>().IsRequired();
        builder.Property(ticket => ticket.Priority).HasConversion<int>().IsRequired();
        builder.Property(ticket => ticket.Status).HasConversion<int>().IsRequired();

        builder.Property(ticket => ticket.CreatedAtUtc).IsRequired();
        builder.Property(ticket => ticket.UpdatedAtUtc).IsRequired();
        builder.Property(ticket => ticket.FirstResponseDueAtUtc).IsRequired();
        builder.Property(ticket => ticket.ResolutionDueAtUtc).IsRequired();

        builder.Property(ticket => ticket.RowVersion).IsRowVersion();

        builder.HasIndex(ticket => ticket.ParentUserId);
        builder.HasIndex(ticket => ticket.Status);
        builder.HasIndex(ticket => ticket.Priority);
        builder.HasIndex(ticket => ticket.Category);
        builder.HasIndex(ticket => ticket.AssignedSupportAgentUserId);
        builder.HasIndex(ticket => ticket.CreatedAtUtc);
        builder.HasIndex(ticket => ticket.AdmissionApplicationId);

        builder.HasIndex(ticket => ticket.SourceContactRequestId)
            .IsUnique()
            .HasFilter("[SourceContactRequestId] IS NOT NULL")
            .HasDatabaseName("IX_SupportTickets_SourceContactRequestId");

        builder.HasMany(ticket => ticket.Messages)
            .WithOne(message => message.Ticket)
            .HasForeignKey(message => message.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ticket => ticket.History)
            .WithOne(entry => entry.Ticket)
            .HasForeignKey(entry => entry.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ticket => ticket.Attachments)
            .WithOne(attachment => attachment.Ticket)
            .HasForeignKey(attachment => attachment.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ticket => ticket.Messages)
            .HasField("_messages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(ticket => ticket.History)
            .HasField("_history")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(ticket => ticket.Attachments)
            .HasField("_attachments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.ToTable("SupportTicketMessages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.AuthorType).HasConversion<int>().IsRequired();
        builder.Property(message => message.Visibility).HasConversion<int>().IsRequired();
        builder.Property(message => message.Body)
            .HasMaxLength(FieldLengthLimits.SupportTicketMessageBody)
            .IsRequired();
        builder.Property(message => message.CreatedAtUtc).IsRequired();

        builder.HasIndex(message => message.TicketId);
        builder.HasIndex(message => message.CreatedAtUtc);
    }
}

public sealed class SupportTicketHistoryConfiguration : IEntityTypeConfiguration<SupportTicketHistory>
{
    public void Configure(EntityTypeBuilder<SupportTicketHistory> builder)
    {
        builder.ToTable("SupportTicketHistory");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Action).HasConversion<int>().IsRequired();
        builder.Property(entry => entry.FromValue).HasMaxLength(FieldLengthLimits.SupportTicketHistoryValue);
        builder.Property(entry => entry.ToValue).HasMaxLength(FieldLengthLimits.SupportTicketHistoryValue);
        builder.Property(entry => entry.Summary).HasMaxLength(FieldLengthLimits.SupportTicketHistorySummary);
        builder.Property(entry => entry.CreatedAtUtc).IsRequired();

        builder.HasIndex(entry => entry.TicketId);
        builder.HasIndex(entry => entry.CreatedAtUtc);
    }
}

public sealed class SupportTicketAttachmentConfiguration : IEntityTypeConfiguration<SupportTicketAttachment>
{
    public void Configure(EntityTypeBuilder<SupportTicketAttachment> builder)
    {
        builder.ToTable("SupportTicketAttachments");
        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Visibility).HasConversion<int>().IsRequired();
        builder.Property(attachment => attachment.StorageKey)
            .HasMaxLength(FieldLengthLimits.SupportTicketStorageKey)
            .IsRequired();
        builder.Property(attachment => attachment.OriginalFileName)
            .HasMaxLength(FieldLengthLimits.SupportTicketFileName)
            .IsRequired();
        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(FieldLengthLimits.SupportTicketContentType)
            .IsRequired();
        builder.Property(attachment => attachment.SizeBytes).IsRequired();
        builder.Property(attachment => attachment.CreatedAtUtc).IsRequired();

        builder.HasIndex(attachment => attachment.TicketId);
        builder.HasIndex(attachment => attachment.MessageId);
    }
}

public sealed class SupportTicketNumberSequenceConfiguration
    : IEntityTypeConfiguration<SupportTicketNumberSequence>
{
    public void Configure(EntityTypeBuilder<SupportTicketNumberSequence> builder)
    {
        builder.ToTable("SupportTicketNumberSequences");
        builder.HasKey(sequence => sequence.DayKey);
        builder.Property(sequence => sequence.DayKey)
            .HasMaxLength(8)
            .IsRequired();
        builder.Property(sequence => sequence.LastValue).IsRequired();
    }
}
