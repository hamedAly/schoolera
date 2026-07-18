using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Active or historical school request for missing Parent-visible application items.</summary>
public sealed class AdmissionMissingItemsRequest
{
    private AdmissionMissingItemsRequest()
    {
        Items = new List<AdmissionMissingItem>();
    }

    public AdmissionMissingItemsRequest(
        Guid admissionApplicationId,
        string parentVisibleReason,
        string? instructions,
        DateTimeOffset? responseDeadlineUtc,
        Guid requestedByUserId)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        ParentVisibleReason = NormalizeRequired(parentVisibleReason, FieldLengthLimits.AdmissionHistoryNote);
        Instructions = NormalizeOptional(instructions, FieldLengthLimits.AdmissionHistoryNote);
        ResponseDeadlineUtc = responseDeadlineUtc;
        RequestedByUserId = requestedByUserId;
        RequestedAtUtc = DateTimeOffset.UtcNow;
        Items = new List<AdmissionMissingItem>();
    }

    public Guid Id { get; private set; }

    public Guid AdmissionApplicationId { get; private set; }

    public string ParentVisibleReason { get; private set; } = null!;

    public string? Instructions { get; private set; }

    public DateTimeOffset? ResponseDeadlineUtc { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; private set; }

    public DateTimeOffset? ClearedAtUtc { get; private set; }

    public Guid? ClearedByUserId { get; private set; }

    public bool IsActive => ClearedAtUtc is null;

    public ICollection<AdmissionMissingItem> Items { get; private set; }

    public void AddItem(AdmissionMissingItem item)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot add items to a cleared missing-items request.");
        }

        Items.Add(item);
    }

    public void Clear(Guid clearedByUserId)
    {
        if (!IsActive)
        {
            return;
        }

        ClearedAtUtc = DateTimeOffset.UtcNow;
        ClearedByUserId = clearedByUserId;
    }

    private static string NormalizeRequired(string value, int max) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", nameof(value))
            : value.Trim().Length <= max
                ? value.Trim()
                : value.Trim()[..max];

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

/// <summary>One explicit missing item referencing an application-owned snapshot or approved field.</summary>
public sealed class AdmissionMissingItem
{
    private AdmissionMissingItem()
    {
    }

    public AdmissionMissingItem(
        Guid missingItemsRequestId,
        AdmissionMissingItemKind kind,
        bool isMandatory,
        string labelAr,
        string labelEn,
        Guid? requirementSnapshotId = null,
        Guid? questionSnapshotId = null,
        AdmissionParentSnapshotFieldCode? parentSnapshotField = null,
        AdmissionChildSnapshotFieldCode? childSnapshotField = null)
    {
        Id = Guid.NewGuid();
        MissingItemsRequestId = missingItemsRequestId;
        Kind = kind;
        IsMandatory = isMandatory;
        LabelAr = Truncate(labelAr, FieldLengthLimits.AdmissionQuestionLabel);
        LabelEn = Truncate(labelEn, FieldLengthLimits.AdmissionQuestionLabel);
        RequirementSnapshotId = requirementSnapshotId;
        QuestionSnapshotId = questionSnapshotId;
        ParentSnapshotField = parentSnapshotField;
        ChildSnapshotField = childSnapshotField;
        IsCompleted = false;
    }

    public Guid Id { get; private set; }

    public Guid MissingItemsRequestId { get; private set; }

    public AdmissionMissingItemKind Kind { get; private set; }

    public bool IsMandatory { get; private set; }

    public string LabelAr { get; private set; } = null!;

    public string LabelEn { get; private set; } = null!;

    public Guid? RequirementSnapshotId { get; private set; }

    public Guid? QuestionSnapshotId { get; private set; }

    public AdmissionParentSnapshotFieldCode? ParentSnapshotField { get; private set; }

    public AdmissionChildSnapshotFieldCode? ChildSnapshotField { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public void MarkCompleted()
    {
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string Truncate(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
