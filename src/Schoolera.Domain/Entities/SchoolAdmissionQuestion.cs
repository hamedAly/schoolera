using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>School-owned dynamic admission question. Mutable until snapshotted onto an application.</summary>
public sealed class SchoolAdmissionQuestion
{
    private SchoolAdmissionQuestion()
    {
        Options = new List<SchoolAdmissionQuestionOption>();
    }

    public SchoolAdmissionQuestion(
        Guid schoolId,
        string questionCode,
        AdmissionQuestionType questionType,
        string labelAr,
        string labelEn,
        string? helpAr,
        string? helpEn,
        bool isRequired,
        int sortOrder,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int? minLength,
        int? maxLength,
        int? minSelectedOptions,
        int? maxSelectedOptions,
        DateOnly? minDate,
        DateOnly? maxDate,
        string? allowedFileExtensions,
        long? maxFileSizeBytes,
        bool allowChildVaultCopy,
        Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        QuestionCode = NormalizeCode(questionCode);
        QuestionType = questionType;
        LabelAr = NormalizeRequired(labelAr, FieldLengthLimits.AdmissionQuestionLabel);
        LabelEn = NormalizeRequired(labelEn, FieldLengthLimits.AdmissionQuestionLabel);
        HelpAr = NormalizeOptional(helpAr, FieldLengthLimits.AdmissionQuestionHelp);
        HelpEn = NormalizeOptional(helpEn, FieldLengthLimits.AdmissionQuestionHelp);
        IsRequired = isRequired;
        SortOrder = sortOrder;
        PublicationStatus = AdmissionQuestionPublicationStatus.Draft;
        IsActive = true;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        MinLength = minLength;
        MaxLength = maxLength;
        MinSelectedOptions = minSelectedOptions;
        MaxSelectedOptions = maxSelectedOptions;
        MinDate = minDate;
        MaxDate = maxDate;
        AllowedFileExtensions = NormalizeOptional(allowedFileExtensions, FieldLengthLimits.AdmissionQuestionAllowedFiles);
        MaxFileSizeBytes = maxFileSizeBytes;
        AllowChildVaultCopy = allowChildVaultCopy;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
        Options = new List<SchoolAdmissionQuestionOption>();
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public School School { get; private set; } = null!;
    public string QuestionCode { get; private set; } = string.Empty;
    public AdmissionQuestionType QuestionType { get; private set; }
    public string LabelAr { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public string? HelpAr { get; private set; }
    public string? HelpEn { get; private set; }
    public bool IsRequired { get; private set; }
    public int SortOrder { get; private set; }
    public AdmissionQuestionPublicationStatus PublicationStatus { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? SchoolBranchId { get; private set; }
    public Guid? EducationalStageId { get; private set; }
    public Guid? GradeId { get; private set; }
    public Guid? AcademicYearId { get; private set; }
    public string ScopeKey { get; private set; } = string.Empty;
    public int? MinLength { get; private set; }
    public int? MaxLength { get; private set; }
    public int? MinSelectedOptions { get; private set; }
    public int? MaxSelectedOptions { get; private set; }
    public DateOnly? MinDate { get; private set; }
    public DateOnly? MaxDate { get; private set; }
    public string? AllowedFileExtensions { get; private set; }
    public long? MaxFileSizeBytes { get; private set; }
    public bool AllowChildVaultCopy { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public ICollection<SchoolAdmissionQuestionOption> Options { get; private set; } = null!;

    public int SpecificityScore =>
        AdmissionScope.ComputeSpecificityScore(SchoolBranchId, EducationalStageId, GradeId, AcademicYearId);

    public void UpdateDraft(
        string labelAr,
        string labelEn,
        string? helpAr,
        string? helpEn,
        bool isRequired,
        int sortOrder,
        Guid? schoolBranchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int? minLength,
        int? maxLength,
        int? minSelectedOptions,
        int? maxSelectedOptions,
        DateOnly? minDate,
        DateOnly? maxDate,
        string? allowedFileExtensions,
        long? maxFileSizeBytes,
        bool allowChildVaultCopy,
        Guid updatedByUserId)
    {
        EnsureDraftEditable();
        LabelAr = NormalizeRequired(labelAr, FieldLengthLimits.AdmissionQuestionLabel);
        LabelEn = NormalizeRequired(labelEn, FieldLengthLimits.AdmissionQuestionLabel);
        HelpAr = NormalizeOptional(helpAr, FieldLengthLimits.AdmissionQuestionHelp);
        HelpEn = NormalizeOptional(helpEn, FieldLengthLimits.AdmissionQuestionHelp);
        IsRequired = isRequired;
        SortOrder = sortOrder;
        SchoolBranchId = schoolBranchId;
        EducationalStageId = educationalStageId;
        GradeId = gradeId;
        AcademicYearId = academicYearId;
        MinLength = minLength;
        MaxLength = maxLength;
        MinSelectedOptions = minSelectedOptions;
        MaxSelectedOptions = maxSelectedOptions;
        MinDate = minDate;
        MaxDate = maxDate;
        AllowedFileExtensions = NormalizeOptional(allowedFileExtensions, FieldLengthLimits.AdmissionQuestionAllowedFiles);
        MaxFileSizeBytes = maxFileSizeBytes;
        AllowChildVaultCopy = allowChildVaultCopy;
        ScopeKey = AdmissionScope.BuildScopeKey(schoolBranchId, educationalStageId, gradeId, academicYearId);
        Touch(updatedByUserId);
    }

    public void SetSortOrder(int sortOrder, Guid updatedByUserId)
    {
        SortOrder = sortOrder;
        Touch(updatedByUserId);
    }

    public void Publish(Guid updatedByUserId)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Inactive questions cannot be published.");
        }

        PublicationStatus = AdmissionQuestionPublicationStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedByUserId);
    }

    public void Unpublish(Guid updatedByUserId)
    {
        PublicationStatus = AdmissionQuestionPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Deactivate(Guid updatedByUserId)
    {
        IsActive = false;
        PublicationStatus = AdmissionQuestionPublicationStatus.Draft;
        Touch(updatedByUserId);
    }

    public void Activate(Guid updatedByUserId)
    {
        IsActive = true;
        Touch(updatedByUserId);
    }

    public void ReplaceOptions(IEnumerable<SchoolAdmissionQuestionOption> options, Guid updatedByUserId)
    {
        EnsureDraftEditable();
        if (QuestionType is not (AdmissionQuestionType.SingleChoice or AdmissionQuestionType.MultipleChoice))
        {
            throw new InvalidOperationException("Options are only allowed for choice questions.");
        }

        Options.Clear();
        foreach (var option in options)
        {
            Options.Add(option);
        }

        Touch(updatedByUserId);
    }

    public static string NormalizeCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > FieldLengthLimits.AdmissionQuestionCode)
        {
            throw new ArgumentException("Question code is invalid.", nameof(code));
        }

        return normalized;
    }

    private void EnsureDraftEditable()
    {
        if (PublicationStatus == AdmissionQuestionPublicationStatus.Published)
        {
            throw new InvalidOperationException("Published questions must be unpublished before structural edits.");
        }
    }

    private void Touch(Guid userId)
    {
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Required text cannot be empty.");
        }

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
