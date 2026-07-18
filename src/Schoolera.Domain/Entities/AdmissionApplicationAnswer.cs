using Schoolera.Domain.Common;

namespace Schoolera.Domain.Entities;

/// <summary>Parent answer for one immutable question snapshot on a Draft (or submitted) application.</summary>
public sealed class AdmissionApplicationAnswer
{
    private AdmissionApplicationAnswer()
    {
    }

    public AdmissionApplicationAnswer(Guid admissionApplicationId, Guid questionSnapshotId)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        QuestionSnapshotId = questionSnapshotId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionApplicationId { get; private set; }
    public AdmissionApplication AdmissionApplication { get; private set; } = null!;
    public Guid QuestionSnapshotId { get; private set; }
    public AdmissionApplicationQuestionSnapshot QuestionSnapshot { get; private set; } = null!;
    public string? TextValue { get; private set; }
    public string? SelectedOptionCodes { get; private set; }
    public DateOnly? DateValue { get; private set; }
    public bool? BooleanValue { get; private set; }
    public Guid? AttachmentId { get; private set; }
    public AdmissionApplicationAttachment? Attachment { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void SetText(string? value)
    {
        ClearValues();
        TextValue = NormalizeText(value);
        Touch();
    }

    public void SetSingleChoice(string optionCode)
    {
        ClearValues();
        SelectedOptionCodes = NormalizeOptionCode(optionCode);
        Touch();
    }

    public void SetMultipleChoice(IEnumerable<string> optionCodes)
    {
        ClearValues();
        var normalized = optionCodes
            .Select(NormalizeOptionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        SelectedOptionCodes = normalized.Length == 0
            ? null
            : string.Join(';', normalized);
        if (SelectedOptionCodes is { Length: > FieldLengthLimits.AdmissionQuestionSelectedOptions })
        {
            throw new ArgumentException("Selected options exceed the platform limit.");
        }

        Touch();
    }

    public void SetDate(DateOnly date)
    {
        ClearValues();
        DateValue = date;
        Touch();
    }

    public void SetYesNo(bool value)
    {
        ClearValues();
        BooleanValue = value;
        Touch();
    }

    public void SetFile(Guid attachmentId)
    {
        ClearValues();
        AttachmentId = attachmentId;
        Touch();
    }

    public void Clear()
    {
        ClearValues();
        Touch();
    }

    public IReadOnlyList<string> GetSelectedOptionCodes()
    {
        if (string.IsNullOrWhiteSpace(SelectedOptionCodes))
        {
            return [];
        }

        return SelectedOptionCodes
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private void ClearValues()
    {
        TextValue = null;
        SelectedOptionCodes = null;
        DateValue = null;
        BooleanValue = null;
        AttachmentId = null;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= FieldLengthLimits.AdmissionQuestionAnswerText
            ? trimmed
            : trimmed[..FieldLengthLimits.AdmissionQuestionAnswerText];
    }

    private static string NormalizeOptionCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Option code is required.");
        }

        return normalized;
    }
}
