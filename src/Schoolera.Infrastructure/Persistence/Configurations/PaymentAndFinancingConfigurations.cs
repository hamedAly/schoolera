using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Configurations;

public sealed class SchoolPayableItemConfiguration : IEntityTypeConfiguration<SchoolPayableItem>
{
    public void Configure(EntityTypeBuilder<SchoolPayableItem> builder)
    {
        builder.ToTable("SchoolPayableItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.PaymentInstructionsAr)
            .HasMaxLength(FieldLengthLimits.PaymentInstructions);
        builder.Property(item => item.PaymentInstructionsEn)
            .HasMaxLength(FieldLengthLimits.PaymentInstructions);
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();
        builder.Property(item => item.RowVersion).IsRowVersion();

        builder.HasIndex(item => item.SchoolId);
        builder.HasIndex(item => item.SchoolBranchId);
        builder.HasIndex(item => item.TuitionFeeId);
        builder.HasIndex(item => new { item.SchoolId, item.TuitionFeeId })
            .IsUnique()
            .HasDatabaseName("IX_SchoolPayableItems_School_TuitionFee");

        builder.HasOne(item => item.TuitionFee)
            .WithMany()
            .HasForeignKey(item => item.TuitionFeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.ToTable("PaymentIntents");
        builder.HasKey(intent => intent.Id);

        builder.Property(intent => intent.Reference)
            .HasMaxLength(FieldLengthLimits.PaymentReference)
            .IsRequired();
        builder.HasIndex(intent => intent.Reference).IsUnique();

        builder.Property(intent => intent.CurrencyCode)
            .HasMaxLength(FieldLengthLimits.PaymentCurrencyCode)
            .IsRequired();
        builder.Property(intent => intent.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(intent => intent.ProviderCode)
            .HasMaxLength(FieldLengthLimits.PaymentProviderCode)
            .IsRequired();
        builder.Property(intent => intent.Environment).HasConversion<int>().IsRequired();
        builder.Property(intent => intent.PaymentMethod).HasConversion<int>().IsRequired();
        builder.Property(intent => intent.Status).HasConversion<int>().IsRequired();
        builder.Property(intent => intent.IdempotencyKey)
            .HasMaxLength(FieldLengthLimits.PaymentIdempotencyKey)
            .IsRequired();
        builder.Property(intent => intent.ProviderCheckoutSessionId)
            .HasMaxLength(FieldLengthLimits.PaymentProviderReference);
        builder.Property(intent => intent.ProviderPaymentReference)
            .HasMaxLength(FieldLengthLimits.PaymentProviderReference);
        builder.Property(intent => intent.RedirectUrl)
            .HasMaxLength(FieldLengthLimits.PaymentRedirectUrl);
        builder.Property(intent => intent.SafeFailureCode)
            .HasMaxLength(FieldLengthLimits.PaymentFailureCode);
        builder.Property(intent => intent.CreatedAtUtc).IsRequired();
        builder.Property(intent => intent.UpdatedAtUtc).IsRequired();
        builder.Property(intent => intent.ExpiresAtUtc).IsRequired();
        builder.Property(intent => intent.RowVersion).IsRowVersion();

        builder.HasIndex(intent => intent.ParentUserId);
        builder.HasIndex(intent => intent.SchoolId);
        builder.HasIndex(intent => intent.PayableItemId);
        builder.HasIndex(intent => intent.Status);
        builder.HasIndex(intent => intent.ProviderCheckoutSessionId);
        builder.HasIndex(intent => new { intent.ParentUserId, intent.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("IX_PaymentIntents_Parent_Idempotency");

        builder.HasMany(intent => intent.Transactions)
            .WithOne()
            .HasForeignKey(tx => tx.PaymentIntentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(intent => intent.Events)
            .WithOne()
            .HasForeignKey(evt => evt.PaymentIntentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(intent => intent.Transactions)
            .HasField("_transactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(intent => intent.Events)
            .HasField("_events")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(tx => tx.Id);
        builder.Property(tx => tx.Type).HasConversion<int>().IsRequired();
        builder.Property(tx => tx.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(tx => tx.CurrencyCode)
            .HasMaxLength(FieldLengthLimits.PaymentCurrencyCode)
            .IsRequired();
        builder.Property(tx => tx.ProviderTransactionId)
            .HasMaxLength(FieldLengthLimits.PaymentProviderReference);
        builder.Property(tx => tx.CreatedAtUtc).IsRequired();
        builder.HasIndex(tx => tx.PaymentIntentId);
        builder.HasIndex(tx => tx.ProviderTransactionId);
    }
}

public sealed class PaymentProviderEventConfiguration : IEntityTypeConfiguration<PaymentProviderEvent>
{
    public void Configure(EntityTypeBuilder<PaymentProviderEvent> builder)
    {
        builder.ToTable("PaymentProviderEvents");
        builder.HasKey(evt => evt.Id);
        builder.Property(evt => evt.ProviderEventId)
            .HasMaxLength(FieldLengthLimits.PaymentProviderReference)
            .IsRequired();
        builder.Property(evt => evt.EventType)
            .HasMaxLength(FieldLengthLimits.PaymentEventType)
            .IsRequired();
        builder.Property(evt => evt.SafeSummary)
            .HasMaxLength(FieldLengthLimits.PaymentSafeSummary);
        builder.Property(evt => evt.ReceivedAtUtc).IsRequired();
        builder.HasIndex(evt => evt.PaymentIntentId);
        builder.HasIndex(evt => evt.FinancingRequestId);
        builder.HasIndex(evt => evt.ProviderEventId)
            .IsUnique()
            .HasDatabaseName("IX_PaymentProviderEvents_ProviderEventId");
    }
}

public sealed class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        builder.ToTable("PaymentReceipts");
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.ReceiptNumber)
            .HasMaxLength(FieldLengthLimits.PaymentReceiptNumber)
            .IsRequired();
        builder.HasIndex(receipt => receipt.ReceiptNumber).IsUnique();
        builder.HasIndex(receipt => receipt.PaymentIntentId).IsUnique();
        builder.Property(receipt => receipt.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(receipt => receipt.CurrencyCode)
            .HasMaxLength(FieldLengthLimits.PaymentCurrencyCode)
            .IsRequired();
        builder.Property(receipt => receipt.Environment).HasConversion<int>().IsRequired();
        builder.Property(receipt => receipt.ProviderPublicName)
            .HasMaxLength(FieldLengthLimits.PaymentProviderCode)
            .IsRequired();
        builder.Property(receipt => receipt.ProviderPaymentReference)
            .HasMaxLength(FieldLengthLimits.PaymentProviderReference);
        builder.Property(receipt => receipt.PaidAtUtc).IsRequired();
        builder.Property(receipt => receipt.CreatedAtUtc).IsRequired();
        builder.HasIndex(receipt => receipt.ParentUserId);
        builder.HasIndex(receipt => receipt.SchoolId);
    }
}

public sealed class PaymentReconciliationRecordConfiguration
    : IEntityTypeConfiguration<PaymentReconciliationRecord>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliationRecord> builder)
    {
        builder.ToTable("PaymentReconciliationRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.ProviderCode)
            .HasMaxLength(FieldLengthLimits.PaymentProviderCode)
            .IsRequired();
        builder.Property(record => record.Environment).HasConversion<int>().IsRequired();
        builder.Property(record => record.MismatchType).HasConversion<int>().IsRequired();
        builder.Property(record => record.Status).HasConversion<int>().IsRequired();
        builder.Property(record => record.InternalStatus)
            .HasMaxLength(FieldLengthLimits.PaymentFailureCode)
            .IsRequired();
        builder.Property(record => record.ProviderStatus)
            .HasMaxLength(FieldLengthLimits.PaymentFailureCode);
        builder.Property(record => record.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(record => record.ProviderAmount).HasPrecision(18, 2);
        builder.Property(record => record.CurrencyCode)
            .HasMaxLength(FieldLengthLimits.PaymentCurrencyCode)
            .IsRequired();
        builder.Property(record => record.SafeResolutionCode)
            .HasMaxLength(FieldLengthLimits.PaymentFailureCode);
        builder.Property(record => record.InternalNote)
            .HasMaxLength(FieldLengthLimits.PaymentReconciliationNote);
        builder.Property(record => record.DetectedAtUtc).IsRequired();
        builder.HasIndex(record => record.Status);
        builder.HasIndex(record => record.PaymentIntentId);
        builder.HasIndex(record => record.FinancingRequestId);
    }
}

public sealed class PaymentReferenceSequenceConfiguration
    : IEntityTypeConfiguration<PaymentReferenceSequence>
{
    public void Configure(EntityTypeBuilder<PaymentReferenceSequence> builder)
    {
        builder.ToTable("PaymentReferenceSequences");
        builder.HasKey(sequence => sequence.DayKey);
        builder.Property(sequence => sequence.DayKey).HasMaxLength(8).IsRequired();
        builder.Property(sequence => sequence.LastValue).IsRequired();
    }
}

public sealed class FinancingRequestConfiguration : IEntityTypeConfiguration<FinancingRequest>
{
    public void Configure(EntityTypeBuilder<FinancingRequest> builder)
    {
        builder.ToTable("FinancingRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.Reference)
            .HasMaxLength(FieldLengthLimits.PaymentReference)
            .IsRequired();
        builder.HasIndex(request => request.Reference).IsUnique();
        builder.Property(request => request.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(request => request.CurrencyCode)
            .HasMaxLength(FieldLengthLimits.PaymentCurrencyCode)
            .IsRequired();
        builder.Property(request => request.ProviderCode)
            .HasMaxLength(FieldLengthLimits.PaymentProviderCode)
            .IsRequired();
        builder.Property(request => request.Environment).HasConversion<int>().IsRequired();
        builder.Property(request => request.Status).HasConversion<int>().IsRequired();
        builder.Property(request => request.IdempotencyKey)
            .HasMaxLength(FieldLengthLimits.PaymentIdempotencyKey)
            .IsRequired();
        builder.Property(request => request.ProviderRequestReference)
            .HasMaxLength(FieldLengthLimits.PaymentProviderReference);
        builder.Property(request => request.CreatedAtUtc).IsRequired();
        builder.Property(request => request.UpdatedAtUtc).IsRequired();
        builder.Property(request => request.RowVersion).IsRowVersion();

        builder.HasIndex(request => request.ParentUserId);
        builder.HasIndex(request => request.SchoolId);
        builder.HasIndex(request => request.PayableItemId);
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => new { request.ParentUserId, request.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("IX_FinancingRequests_Parent_Idempotency");

        builder.HasMany(request => request.Offers)
            .WithOne()
            .HasForeignKey(offer => offer.FinancingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(request => request.Decisions)
            .WithOne()
            .HasForeignKey(decision => decision.FinancingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(request => request.Offers)
            .HasField("_offers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(request => request.Decisions)
            .HasField("_decisions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class FinancingOfferConfiguration : IEntityTypeConfiguration<FinancingOffer>
{
    public void Configure(EntityTypeBuilder<FinancingOffer> builder)
    {
        builder.ToTable("FinancingOffers");
        builder.HasKey(offer => offer.Id);
        builder.Property(offer => offer.ProviderOfferReference)
            .HasMaxLength(FieldLengthLimits.PaymentProviderReference)
            .IsRequired();
        builder.Property(offer => offer.PeriodicInstallment).HasPrecision(18, 2).IsRequired();
        builder.Property(offer => offer.TotalRepayment).HasPrecision(18, 2).IsRequired();
        builder.Property(offer => offer.Fees).HasPrecision(18, 2);
        builder.Property(offer => offer.InterestOrProfitRate).HasPrecision(9, 4);
        builder.Property(offer => offer.AprProviderReported).HasPrecision(9, 4);
        builder.Property(offer => offer.DownPayment).HasPrecision(18, 2);
        builder.Property(offer => offer.DisclosureText)
            .HasMaxLength(FieldLengthLimits.FinancingDisclosure);
        builder.Property(offer => offer.Status).HasConversion<int>().IsRequired();
        builder.Property(offer => offer.ExpiresAtUtc).IsRequired();
        builder.Property(offer => offer.CreatedAtUtc).IsRequired();
        builder.HasIndex(offer => offer.FinancingRequestId);
        builder.HasIndex(offer => offer.ProviderOfferReference);
    }
}

public sealed class FinancingDecisionEventConfiguration : IEntityTypeConfiguration<FinancingDecisionEvent>
{
    public void Configure(EntityTypeBuilder<FinancingDecisionEvent> builder)
    {
        builder.ToTable("FinancingDecisionEvents");
        builder.HasKey(evt => evt.Id);
        builder.Property(evt => evt.DecisionType)
            .HasMaxLength(FieldLengthLimits.PaymentEventType)
            .IsRequired();
        builder.Property(evt => evt.SafeSummary)
            .HasMaxLength(FieldLengthLimits.PaymentSafeSummary);
        builder.Property(evt => evt.CreatedAtUtc).IsRequired();
        builder.HasIndex(evt => evt.FinancingRequestId);
    }
}
