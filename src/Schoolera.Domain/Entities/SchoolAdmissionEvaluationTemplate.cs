using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

public sealed class SchoolAdmissionEvaluationTemplate
{
    private readonly List<SchoolAdmissionEvaluationCriterion> criteria = [];

    private SchoolAdmissionEvaluationTemplate() { }

    public SchoolAdmissionEvaluationTemplate(
        Guid schoolId,
        string nameAr,
        string nameEn,
        EvaluationTemplateKind kind,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        Guid actorUserId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        PublicationStatus = EvaluationTemplatePublicationStatus.Draft;
        IsActive = true;
        Version = 1;
        CreatedByUserId = UpdatedByUserId = actorUserId;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdateDraft(nameAr, nameEn, kind, branchId, stageId, gradeId, academicYearId, actorUserId);
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid? SchoolBranchId { get; private set; }
    public Guid? EducationalStageId { get; private set; }
    public Guid? GradeId { get; private set; }
    public Guid? AcademicYearId { get; private set; }
    public string ScopeKey { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public EvaluationTemplateKind Kind { get; private set; }
    public EvaluationTemplatePublicationStatus PublicationStatus { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<SchoolAdmissionEvaluationCriterion> Criteria => criteria;
    public int SpecificityScore => AdmissionScope.ComputeSpecificityScore(
        SchoolBranchId, EducationalStageId, GradeId, AcademicYearId);

    public void UpdateDraft(
        string nameAr,
        string nameEn,
        EvaluationTemplateKind kind,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        Guid actorUserId)
    {
        EnsureDraft();
        NameAr = Required(nameAr, 200);
        NameEn = Required(nameEn, 200);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
        SchoolBranchId = branchId;
        EducationalStageId = stageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        ScopeKey = AdmissionScope.BuildScopeKey(branchId, stageId, gradeId, academicYearId);
        Touch(actorUserId);
    }

    public void ReplaceCriteria(IEnumerable<SchoolAdmissionEvaluationCriterion> replacements, Guid actorUserId)
    {
        EnsureDraft();
        criteria.Clear();
        criteria.AddRange(replacements.OrderBy(x => x.SortOrder));
        Touch(actorUserId);
    }

    public void Publish(Guid actorUserId)
    {
        EnsureDraft();
        if (!IsActive || criteria.Count == 0)
            throw new InvalidOperationException("An active template with criteria is required.");
        if (PublishedAtUtc.HasValue) Version++;
        PublicationStatus = EvaluationTemplatePublicationStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        Touch(actorUserId);
    }

    public void Unpublish(Guid actorUserId)
    {
        PublicationStatus = EvaluationTemplatePublicationStatus.Draft;
        Touch(actorUserId);
    }

    public void Deactivate(Guid actorUserId)
    {
        IsActive = false;
        PublicationStatus = EvaluationTemplatePublicationStatus.Draft;
        Touch(actorUserId);
    }

    public SchoolAdmissionEvaluationTemplate CloneAsDraft(Guid actorUserId)
    {
        var clone = new SchoolAdmissionEvaluationTemplate(
            SchoolId, NameAr, NameEn, Kind, SchoolBranchId, EducationalStageId,
            GradeId, AcademicYearId, actorUserId);
        clone.ReplaceCriteria(criteria.Select(x => x.CloneFor(clone.Id)), actorUserId);
        return clone;
    }

    private void EnsureDraft()
    {
        if (PublicationStatus != EvaluationTemplatePublicationStatus.Draft)
            throw new InvalidOperationException("Published templates must be unpublished before editing.");
    }

    private void Touch(Guid actorUserId)
    {
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.");
        var normalized = value.Trim();
        return normalized[..Math.Min(max, normalized.Length)];
    }
}

public sealed class SchoolAdmissionEvaluationCriterion
{
    private readonly List<SchoolAdmissionEvaluationCriterionOption> options = [];

    private SchoolAdmissionEvaluationCriterion() { }

    public SchoolAdmissionEvaluationCriterion(
        Guid templateId,
        EvaluationCriterionType type,
        string labelAr,
        string labelEn,
        string? helpTextAr,
        string? helpTextEn,
        bool isRequired,
        int sortOrder,
        int? shortTextMaxLength,
        IEnumerable<(string Value, string LabelAr, string LabelEn)> controlledOptions)
    {
        Id = Guid.NewGuid();
        SchoolAdmissionEvaluationTemplateId = templateId;
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        Type = type;
        LabelAr = Required(labelAr, 300);
        LabelEn = Required(labelEn, 300);
        HelpTextAr = Optional(helpTextAr, 1000);
        HelpTextEn = Optional(helpTextEn, 1000);
        IsRequired = isRequired;
        SortOrder = sortOrder;
        ShortTextMaxLength = type == EvaluationCriterionType.ShortText
            ? Math.Clamp(shortTextMaxLength ?? 500, 1, 2000)
            : null;
        if (type == EvaluationCriterionType.SingleChoice)
        {
            foreach (var option in controlledOptions)
                options.Add(new SchoolAdmissionEvaluationCriterionOption(
                    Id, option.Value, option.LabelAr, option.LabelEn, options.Count));
            if (options.Count < 2) throw new ArgumentException("Single choice requires at least two options.");
        }
    }

    public Guid Id { get; private set; }
    public Guid SchoolAdmissionEvaluationTemplateId { get; private set; }
    public EvaluationCriterionType Type { get; private set; }
    public string LabelAr { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public string? HelpTextAr { get; private set; }
    public string? HelpTextEn { get; private set; }
    public bool IsRequired { get; private set; }
    public int SortOrder { get; private set; }
    public int? ShortTextMaxLength { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<SchoolAdmissionEvaluationCriterionOption> Options => options;

    public SchoolAdmissionEvaluationCriterion CloneFor(Guid templateId) =>
        new(templateId, Type, LabelAr, LabelEn, HelpTextAr, HelpTextEn, IsRequired,
            SortOrder, ShortTextMaxLength,
            options.OrderBy(x => x.SortOrder).Select(x => (x.Value, x.LabelAr, x.LabelEn)));

    private static string Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.");
        var normalized = value.Trim();
        return normalized[..Math.Min(max, normalized.Length)];
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized[..Math.Min(max, normalized.Length)];
    }
}

public sealed class SchoolAdmissionEvaluationCriterionOption
{
    private SchoolAdmissionEvaluationCriterionOption() { }

    public SchoolAdmissionEvaluationCriterionOption(
        Guid criterionId, string value, string labelAr, string labelEn, int sortOrder)
    {
        Id = Guid.NewGuid();
        SchoolAdmissionEvaluationCriterionId = criterionId;
        Value = Required(value, 100);
        LabelAr = Required(labelAr, 200);
        LabelEn = Required(labelEn, 200);
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public Guid SchoolAdmissionEvaluationCriterionId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string LabelAr { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private static string Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.");
        var normalized = value.Trim();
        return normalized[..Math.Min(max, normalized.Length)];
    }
}

public sealed class SchoolAdmissionEvaluationTemplateAudit
{
    private SchoolAdmissionEvaluationTemplateAudit() { }

    public SchoolAdmissionEvaluationTemplateAudit(
        Guid schoolId, Guid templateId, string action, Guid actorUserId, int version,
        string? idempotencyKey = null)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        SchoolAdmissionEvaluationTemplateId = templateId;
        Action = string.IsNullOrWhiteSpace(action)
            ? throw new ArgumentException("Action is required.")
            : action.Trim()[..Math.Min(80, action.Trim().Length)];
        ActorUserId = actorUserId;
        Version = version;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim()[..Math.Min(128, idempotencyKey.Trim().Length)];
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid SchoolAdmissionEvaluationTemplateId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public int Version { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
