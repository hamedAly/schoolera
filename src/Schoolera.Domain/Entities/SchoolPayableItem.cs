using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// School-configured platform-payable item backed by a published informational fee.
/// Amount is always resolved from the linked fee at intent time (server-side).
/// </summary>
public sealed class SchoolPayableItem
{
    private SchoolPayableItem()
    {
    }

    public SchoolPayableItem(
        Guid schoolId,
        Guid schoolBranchId,
        Guid tuitionFeeId,
        DateTimeOffset? payableFromUtc,
        DateTimeOffset? payableToUtc,
        string? paymentInstructionsAr,
        string? paymentInstructionsEn)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        SchoolBranchId = schoolBranchId;
        TuitionFeeId = tuitionFeeId;
        PayableFromUtc = payableFromUtc;
        PayableToUtc = payableToUtc;
        PaymentInstructionsAr = Normalize(paymentInstructionsAr);
        PaymentInstructionsEn = Normalize(paymentInstructionsEn);
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid SchoolBranchId { get; private set; }

    public Guid TuitionFeeId { get; private set; }

    public TuitionFee TuitionFee { get; private set; } = null!;

    public DateTimeOffset? PayableFromUtc { get; private set; }

    public DateTimeOffset? PayableToUtc { get; private set; }

    public string? PaymentInstructionsAr { get; private set; }

    public string? PaymentInstructionsEn { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void UpdateWindow(
        DateTimeOffset? payableFromUtc,
        DateTimeOffset? payableToUtc,
        string? paymentInstructionsAr,
        string? paymentInstructionsEn)
    {
        PayableFromUtc = payableFromUtc;
        PayableToUtc = payableToUtc;
        PaymentInstructionsAr = Normalize(paymentInstructionsAr);
        PaymentInstructionsEn = Normalize(paymentInstructionsEn);
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

    public bool IsCurrentlyPayable(DateTimeOffset utcNow) =>
        IsActive &&
        (PayableFromUtc is null || PayableFromUtc <= utcNow) &&
        (PayableToUtc is null || PayableToUtc >= utcNow);

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
