using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Informational installment-display row for a fee. Not a payment schedule obligation.</summary>
public sealed class SchoolFeeInstallmentDisplay
{
    private SchoolFeeInstallmentDisplay()
    {
    }

    public SchoolFeeInstallmentDisplay(
        Guid tuitionFeeId,
        int sequenceNumber,
        string nameAr,
        string? nameEn,
        FeeInstallmentAmountMode amountMode,
        decimal? fixedAmount,
        decimal? percentage,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset? dueWindowStartUtc,
        DateTimeOffset? dueWindowEndUtc,
        string? notesAr,
        string? notesEn,
        int sortOrder)
    {
        ValidateAmount(amountMode, fixedAmount, percentage);
        ValidateWindow(dueWindowStartUtc, dueWindowEndUtc);

        Id = Guid.NewGuid();
        TuitionFeeId = tuitionFeeId;
        SequenceNumber = sequenceNumber;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        AmountMode = amountMode;
        FixedAmount = amountMode == FeeInstallmentAmountMode.FixedAmount ? fixedAmount : null;
        Percentage = amountMode == FeeInstallmentAmountMode.Percentage ? percentage : null;
        DueDateUtc = dueDateUtc;
        DueWindowStartUtc = dueWindowStartUtc;
        DueWindowEndUtc = dueWindowEndUtc;
        NotesAr = string.IsNullOrWhiteSpace(notesAr) ? null : notesAr.Trim();
        NotesEn = string.IsNullOrWhiteSpace(notesEn) ? null : notesEn.Trim();
        SortOrder = sortOrder;
        IsActive = true;
        IsPublished = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TuitionFeeId { get; private set; }

    public TuitionFee TuitionFee { get; private set; } = null!;

    public int SequenceNumber { get; private set; }

    public string NameAr { get; private set; } = string.Empty;

    public string? NameEn { get; private set; }

    public FeeInstallmentAmountMode AmountMode { get; private set; }

    public decimal? FixedAmount { get; private set; }

    public decimal? Percentage { get; private set; }

    public DateTimeOffset? DueDateUtc { get; private set; }

    public DateTimeOffset? DueWindowStartUtc { get; private set; }

    public DateTimeOffset? DueWindowEndUtc { get; private set; }

    public string? NotesAr { get; private set; }

    public string? NotesEn { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        int sequenceNumber,
        string nameAr,
        string? nameEn,
        FeeInstallmentAmountMode amountMode,
        decimal? fixedAmount,
        decimal? percentage,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset? dueWindowStartUtc,
        DateTimeOffset? dueWindowEndUtc,
        string? notesAr,
        string? notesEn,
        int sortOrder)
    {
        ValidateAmount(amountMode, fixedAmount, percentage);
        ValidateWindow(dueWindowStartUtc, dueWindowEndUtc);

        SequenceNumber = sequenceNumber;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        AmountMode = amountMode;
        FixedAmount = amountMode == FeeInstallmentAmountMode.FixedAmount ? fixedAmount : null;
        Percentage = amountMode == FeeInstallmentAmountMode.Percentage ? percentage : null;
        DueDateUtc = dueDateUtc;
        DueWindowStartUtc = dueWindowStartUtc;
        DueWindowEndUtc = dueWindowEndUtc;
        NotesAr = string.IsNullOrWhiteSpace(notesAr) ? null : notesAr.Trim();
        NotesEn = string.IsNullOrWhiteSpace(notesEn) ? null : notesEn.Trim();
        SortOrder = sortOrder;
        Touch();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        Touch();
    }

    public void Publish()
    {
        IsPublished = true;
        Touch();
    }

    public void Unpublish()
    {
        IsPublished = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public static void ValidateAmount(
        FeeInstallmentAmountMode mode,
        decimal? fixedAmount,
        decimal? percentage)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (mode == FeeInstallmentAmountMode.FixedAmount)
        {
            if (fixedAmount is null || fixedAmount < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedAmount));
            }

            if (percentage is not null)
            {
                throw new ArgumentException("Percentage must be null when AmountMode is FixedAmount.");
            }
        }
        else
        {
            if (percentage is null || percentage <= 0m || percentage > 100m)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage));
            }

            if (fixedAmount is not null)
            {
                throw new ArgumentException("FixedAmount must be null when AmountMode is Percentage.");
            }
        }
    }

    public static void ValidateWindow(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start is { } s && end is { } e && e < s)
        {
            throw new ArgumentException("DueWindowEndUtc must be on or after DueWindowStartUtc.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
