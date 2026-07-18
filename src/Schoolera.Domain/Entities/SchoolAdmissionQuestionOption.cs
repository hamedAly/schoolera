using Schoolera.Domain.Common;

namespace Schoolera.Domain.Entities;

public sealed class SchoolAdmissionQuestionOption
{
    private SchoolAdmissionQuestionOption()
    {
    }

    public SchoolAdmissionQuestionOption(
        Guid schoolAdmissionQuestionId,
        string optionCode,
        string labelAr,
        string labelEn,
        int sortOrder,
        bool isActive = true)
    {
        Id = Guid.NewGuid();
        SchoolAdmissionQuestionId = schoolAdmissionQuestionId;
        OptionCode = NormalizeCode(optionCode);
        LabelAr = NormalizeRequired(labelAr);
        LabelEn = NormalizeRequired(labelEn);
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public Guid SchoolAdmissionQuestionId { get; private set; }
    public SchoolAdmissionQuestion Question { get; private set; } = null!;
    public string OptionCode { get; private set; } = string.Empty;
    public string LabelAr { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string labelAr, string labelEn, int sortOrder, bool isActive)
    {
        LabelAr = NormalizeRequired(labelAr);
        LabelEn = NormalizeRequired(labelEn);
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public static string NormalizeCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > FieldLengthLimits.AdmissionQuestionOptionCode)
        {
            throw new ArgumentException("Option code is invalid.", nameof(code));
        }

        return normalized;
    }

    private static string NormalizeRequired(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Option label cannot be empty.");
        }

        return trimmed.Length <= FieldLengthLimits.AdmissionQuestionOptionLabel
            ? trimmed
            : trimmed[..FieldLengthLimits.AdmissionQuestionOptionLabel];
    }
}
