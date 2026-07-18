using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Informational published discount. Does not auto-evaluate Parent eligibility.</summary>
public sealed class SchoolPublishedDiscount
{
    private SchoolPublishedDiscount()
    {
    }

    public SchoolPublishedDiscount(
        Guid schoolId,
        string titleAr,
        string? titleEn,
        string eligibilityDescriptionAr,
        string? eligibilityDescriptionEn,
        FeeDiscountType discountType,
        decimal value,
        string? currencyCode,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int sortOrder)
    {
        Validate(discountType, value, currencyCode, startUtc, endUtc);

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TitleAr = titleAr.Trim();
        TitleEn = string.IsNullOrWhiteSpace(titleEn) ? null : titleEn.Trim();
        EligibilityDescriptionAr = eligibilityDescriptionAr.Trim();
        EligibilityDescriptionEn = string.IsNullOrWhiteSpace(eligibilityDescriptionEn)
            ? null
            : eligibilityDescriptionEn.Trim();
        DiscountType = discountType;
        Value = value;
        CurrencyCode = discountType == FeeDiscountType.FixedAmount
            ? currencyCode!.Trim().ToUpperInvariant()
            : null;
        StartUtc = startUtc;
        EndUtc = endUtc;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        SortOrder = sortOrder;
        IsActive = true;
        IsPublished = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public School School { get; private set; } = null!;

    public string TitleAr { get; private set; } = string.Empty;

    public string? TitleEn { get; private set; }

    public string EligibilityDescriptionAr { get; private set; } = string.Empty;

    public string? EligibilityDescriptionEn { get; private set; }

    public FeeDiscountType DiscountType { get; private set; }

    public decimal Value { get; private set; }

    public string? CurrencyCode { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public Guid? SchoolBranchId { get; private set; }

    public SchoolBranch? SchoolBranch { get; private set; }

    public Guid? EducationalStageId { get; private set; }

    public EducationalStage? EducationalStage { get; private set; }

    public Guid? GradeId { get; private set; }

    public Grade? Grade { get; private set; }

    public Guid? AcademicYearId { get; private set; }

    public AcademicYear? AcademicYear { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string titleAr,
        string? titleEn,
        string eligibilityDescriptionAr,
        string? eligibilityDescriptionEn,
        FeeDiscountType discountType,
        decimal value,
        string? currencyCode,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int sortOrder)
    {
        Validate(discountType, value, currencyCode, startUtc, endUtc);

        TitleAr = titleAr.Trim();
        TitleEn = string.IsNullOrWhiteSpace(titleEn) ? null : titleEn.Trim();
        EligibilityDescriptionAr = eligibilityDescriptionAr.Trim();
        EligibilityDescriptionEn = string.IsNullOrWhiteSpace(eligibilityDescriptionEn)
            ? null
            : eligibilityDescriptionEn.Trim();
        DiscountType = discountType;
        Value = value;
        CurrencyCode = discountType == FeeDiscountType.FixedAmount
            ? currencyCode!.Trim().ToUpperInvariant()
            : null;
        StartUtc = startUtc;
        EndUtc = endUtc;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
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

    public bool IsCurrent(DateTimeOffset utcNow) =>
        IsActive && IsPublished && utcNow >= StartUtc && utcNow <= EndUtc;

    public bool IsUpcoming(DateTimeOffset utcNow) =>
        IsActive && IsPublished && utcNow < StartUtc;

    public bool IsExpired(DateTimeOffset utcNow) =>
        utcNow > EndUtc || (!IsActive && IsPublished);

    public static void Validate(
        FeeDiscountType type,
        decimal value,
        string? currencyCode,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (endUtc < startUtc)
        {
            throw new ArgumentException("EndUtc must be on or after StartUtc.");
        }

        if (type == FeeDiscountType.FixedAmount)
        {
            if (value <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            {
                throw new ArgumentException("CurrencyCode is required for FixedAmount discounts.");
            }
        }
        else
        {
            if (value <= 0m || value > 100m)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
