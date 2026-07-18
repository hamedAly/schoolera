using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>
/// Informational school fee row (tuition and related categories). Not a payment obligation.
/// </summary>
public sealed class TuitionFee
{
    private TuitionFee()
    {
        Installments = new List<SchoolFeeInstallmentDisplay>();
    }

    public TuitionFee(
        Guid schoolBranchId,
        Guid educationalStageId,
        Guid? gradeId,
        Guid academicYearId,
        FeeCategory category,
        string currencyCode,
        decimal amount,
        string? nameAr = null,
        string? nameEn = null,
        bool isStartingFrom = false,
        int sortOrder = 0)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Id = Guid.NewGuid();
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        Category = category;
        NameAr = NormalizeOptional(nameAr);
        NameEn = NormalizeOptional(nameEn);
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Amount = amount;
        IsStartingFrom = isStartingFrom;
        SortOrder = sortOrder;
        IsActive = true;
        IsPublished = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        Installments = new List<SchoolFeeInstallmentDisplay>();
    }

    public Guid Id { get; private set; }

    public Guid SchoolBranchId { get; private set; }

    public SchoolBranch SchoolBranch { get; private set; } = null!;

    public Guid EducationalStageId { get; private set; }

    public EducationalStage EducationalStage { get; private set; } = null!;

    public Guid? GradeId { get; private set; }

    public Grade? Grade { get; private set; }

    public Guid AcademicYearId { get; private set; }

    public AcademicYear AcademicYear { get; private set; } = null!;

    public FeeCategory Category { get; private set; }

    public string? NameAr { get; private set; }

    public string? NameEn { get; private set; }

    public string CurrencyCode { get; private set; } = "EGP";

    public decimal Amount { get; private set; }

    public bool IsStartingFrom { get; private set; }

    /// <summary>Parent-visible informational notes (legacy Notes* columns).</summary>
    public string? NotesAr { get; private set; }

    public string? NotesEn { get; private set; }

    /// <summary>Internal school notes — never exposed on public/Parent DTOs.</summary>
    public string? InternalNotesAr { get; private set; }

    public string? InternalNotesEn { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? EffectiveFromUtc { get; private set; }

    public DateTimeOffset? EffectiveToUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<SchoolFeeInstallmentDisplay> Installments { get; private set; }

    public void Update(
        Guid educationalStageId,
        Guid? gradeId,
        Guid academicYearId,
        FeeCategory category,
        string? nameAr,
        string? nameEn,
        string currencyCode,
        decimal amount,
        bool isStartingFrom,
        string? notesAr,
        string? notesEn,
        string? internalNotesAr,
        string? internalNotesEn,
        int sortOrder,
        DateTimeOffset? effectiveFromUtc,
        DateTimeOffset? effectiveToUtc)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        ValidateEffectiveRange(effectiveFromUtc, effectiveToUtc);

        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        Category = category;
        NameAr = NormalizeOptional(nameAr);
        NameEn = NormalizeOptional(nameEn);
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Amount = amount;
        IsStartingFrom = isStartingFrom;
        NotesAr = NormalizeOptional(notesAr);
        NotesEn = NormalizeOptional(notesEn);
        InternalNotesAr = NormalizeOptional(internalNotesAr);
        InternalNotesEn = NormalizeOptional(internalNotesEn);
        SortOrder = sortOrder;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        Touch();
    }

    public void SetNotes(string? notesAr, string? notesEn)
    {
        NotesAr = NormalizeOptional(notesAr);
        NotesEn = NormalizeOptional(notesEn);
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

    public bool IsCurrentlyEffective(DateTimeOffset utcNow)
    {
        if (EffectiveFromUtc is { } from && utcNow < from)
        {
            return false;
        }

        if (EffectiveToUtc is { } to && utcNow > to)
        {
            return false;
        }

        return true;
    }

    public static void ValidateEffectiveRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is { } f && to is { } t && t < f)
        {
            throw new ArgumentException("EffectiveToUtc must be on or after EffectiveFromUtc.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
