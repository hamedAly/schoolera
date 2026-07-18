using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Domain.Entities;

/// <summary>Immutable per-application copy of a school question resolved when the questions step loads.</summary>
public sealed class AdmissionApplicationQuestionSnapshot
{
    private AdmissionApplicationQuestionSnapshot()
    {
        Options = new List<AdmissionApplicationQuestionSnapshotOption>();
        Attachments = new List<AdmissionApplicationAttachment>();
    }

    public AdmissionApplicationQuestionSnapshot(
        Guid admissionApplicationId,
        Guid? sourceQuestionId,
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
        bool allowChildVaultCopy)
    {
        Id = Guid.NewGuid();
        AdmissionApplicationId = admissionApplicationId;
        SourceQuestionId = sourceQuestionId;
        QuestionCode = questionCode.Trim().ToLowerInvariant();
        QuestionType = questionType;
        LabelAr = Truncate(labelAr, FieldLengthLimits.AdmissionQuestionLabel);
        LabelEn = Truncate(labelEn, FieldLengthLimits.AdmissionQuestionLabel);
        HelpAr = TruncateOptional(helpAr, FieldLengthLimits.AdmissionQuestionHelp);
        HelpEn = TruncateOptional(helpEn, FieldLengthLimits.AdmissionQuestionHelp);
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
        AllowedFileExtensions = TruncateOptional(allowedFileExtensions, FieldLengthLimits.AdmissionQuestionAllowedFiles);
        MaxFileSizeBytes = maxFileSizeBytes;
        AllowChildVaultCopy = allowChildVaultCopy;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Options = new List<AdmissionApplicationQuestionSnapshotOption>();
        Attachments = new List<AdmissionApplicationAttachment>();
    }

    public static AdmissionApplicationQuestionSnapshot FromDefinition(
        Guid admissionApplicationId,
        SchoolAdmissionQuestion question)
    {
        var snapshot = new AdmissionApplicationQuestionSnapshot(
            admissionApplicationId,
            question.Id,
            question.QuestionCode,
            question.QuestionType,
            question.LabelAr,
            question.LabelEn,
            question.HelpAr,
            question.HelpEn,
            question.IsRequired,
            question.SortOrder,
            question.SchoolBranchId,
            question.EducationalStageId,
            question.GradeId,
            question.AcademicYearId,
            question.MinLength,
            question.MaxLength,
            question.MinSelectedOptions,
            question.MaxSelectedOptions,
            question.MinDate,
            question.MaxDate,
            question.AllowedFileExtensions,
            question.MaxFileSizeBytes,
            question.AllowChildVaultCopy);

        foreach (var option in question.Options
                     .Where(item => item.IsActive)
                     .OrderBy(item => item.SortOrder)
                     .ThenBy(item => item.OptionCode))
        {
            snapshot.Options.Add(new AdmissionApplicationQuestionSnapshotOption(
                snapshot.Id,
                option.OptionCode,
                option.LabelAr,
                option.LabelEn,
                option.SortOrder));
        }

        return snapshot;
    }

    public Guid Id { get; private set; }
    public Guid AdmissionApplicationId { get; private set; }
    public AdmissionApplication AdmissionApplication { get; private set; } = null!;
    public Guid? SourceQuestionId { get; private set; }
    public string QuestionCode { get; private set; } = string.Empty;
    public AdmissionQuestionType QuestionType { get; private set; }
    public string LabelAr { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public string? HelpAr { get; private set; }
    public string? HelpEn { get; private set; }
    public bool IsRequired { get; private set; }
    public int SortOrder { get; private set; }
    public Guid? SchoolBranchId { get; private set; }
    public Guid? EducationalStageId { get; private set; }
    public Guid? GradeId { get; private set; }
    public Guid? AcademicYearId { get; private set; }
    public int? MinLength { get; private set; }
    public int? MaxLength { get; private set; }
    public int? MinSelectedOptions { get; private set; }
    public int? MaxSelectedOptions { get; private set; }
    public DateOnly? MinDate { get; private set; }
    public DateOnly? MaxDate { get; private set; }
    public string? AllowedFileExtensions { get; private set; }
    public long? MaxFileSizeBytes { get; private set; }
    public bool AllowChildVaultCopy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public ICollection<AdmissionApplicationQuestionSnapshotOption> Options { get; private set; } = null!;
    public ICollection<AdmissionApplicationAttachment> Attachments { get; private set; } = null!;
    public AdmissionApplicationAnswer? Answer { get; private set; }

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? TruncateOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Truncate(value, maxLength);
    }
}
