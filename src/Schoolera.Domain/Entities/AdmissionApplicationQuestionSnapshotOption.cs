using Schoolera.Domain.Common;

namespace Schoolera.Domain.Entities;

public sealed class AdmissionApplicationQuestionSnapshotOption
{
    private AdmissionApplicationQuestionSnapshotOption()
    {
    }

    public AdmissionApplicationQuestionSnapshotOption(
        Guid questionSnapshotId,
        string optionCode,
        string labelAr,
        string labelEn,
        int sortOrder)
    {
        Id = Guid.NewGuid();
        QuestionSnapshotId = questionSnapshotId;
        OptionCode = optionCode.Trim().ToLowerInvariant();
        LabelAr = Truncate(labelAr);
        LabelEn = Truncate(labelEn);
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public Guid QuestionSnapshotId { get; private set; }
    public AdmissionApplicationQuestionSnapshot QuestionSnapshot { get; private set; } = null!;
    public string OptionCode { get; private set; } = string.Empty;
    public string LabelAr { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private static string Truncate(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= FieldLengthLimits.AdmissionQuestionOptionLabel
            ? trimmed
            : trimmed[..FieldLengthLimits.AdmissionQuestionOptionLabel];
    }
}
