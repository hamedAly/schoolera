using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Platform allowlists and scope resolution for dynamic admission questions.</summary>
public static class AdmissionQuestionCatalog
{
    public const int MaxExportAnswerColumns = 30;

    public static bool IsChoiceType(AdmissionQuestionType type) =>
        type is AdmissionQuestionType.SingleChoice or AdmissionQuestionType.MultipleChoice;

    public static bool IsTextType(AdmissionQuestionType type) =>
        type is AdmissionQuestionType.ShortText or AdmissionQuestionType.LongText;

    public static int DefaultMaxLength(AdmissionQuestionType type) =>
        type switch
        {
            AdmissionQuestionType.ShortText => FieldLengthLimits.AdmissionQuestionShortTextMax,
            AdmissionQuestionType.LongText => FieldLengthLimits.AdmissionQuestionLongTextMax,
            _ => FieldLengthLimits.AdmissionQuestionAnswerText,
        };

    public static string WizardSection => "questions";

    public static IReadOnlyList<SchoolAdmissionQuestion> ResolveApplicable(
        IEnumerable<SchoolAdmissionQuestion> candidates,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId)
    {
        var matching = candidates
            .Where(question =>
                question.IsActive &&
                question.PublicationStatus == AdmissionQuestionPublicationStatus.Published &&
                AdmissionScope.MatchesScope(
                    question.SchoolBranchId,
                    question.EducationalStageId,
                    question.GradeId,
                    question.AcademicYearId,
                    branchId,
                    stageId,
                    gradeId,
                    academicYearId))
            .ToList();

        return matching
            .GroupBy(question => question.QuestionCode, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group
                    .OrderByDescending(question => question.SpecificityScore)
                    .ThenBy(question => question.Id)
                    .ToList();
                return ordered[0];
            })
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.QuestionCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> ValidateForPublish(SchoolAdmissionQuestion question)
    {
        var errors = new List<string>();
        if (question.SpecificityScore <= 0)
        {
            errors.Add("invalidScope");
        }

        if (!Enum.IsDefined(question.QuestionType))
        {
            errors.Add("invalidType");
        }

        if (IsTextType(question.QuestionType))
        {
            var maxCap = DefaultMaxLength(question.QuestionType);
            if (question.MaxLength is { } max && (max < 1 || max > maxCap))
            {
                errors.Add("invalidMaxLength");
            }

            if (question.MinLength is { } min && min < 0)
            {
                errors.Add("invalidMinLength");
            }

            if (question.MinLength is { } minLen &&
                question.MaxLength is { } maxLen &&
                minLen > maxLen)
            {
                errors.Add("invalidLengthRange");
            }
        }

        if (IsChoiceType(question.QuestionType))
        {
            var activeOptions = question.Options.Where(option => option.IsActive).ToList();
            if (activeOptions.Count == 0)
            {
                errors.Add("optionsRequired");
            }

            if (activeOptions.Count > FieldLengthLimits.AdmissionQuestionMaxOptions)
            {
                errors.Add("tooManyOptions");
            }

            if (activeOptions.Select(option => option.OptionCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != activeOptions.Count)
            {
                errors.Add("duplicateOptionCodes");
            }

            if (question.QuestionType == AdmissionQuestionType.MultipleChoice)
            {
                if (question.MaxSelectedOptions is { } maxSel &&
                    (maxSel < 1 || maxSel > FieldLengthLimits.AdmissionQuestionMaxMultiSelect))
                {
                    errors.Add("invalidMaxSelected");
                }

                if (question.MinSelectedOptions is { } minSel && minSel < 0)
                {
                    errors.Add("invalidMinSelected");
                }

                if (question.MinSelectedOptions is { } minS &&
                    question.MaxSelectedOptions is { } maxS &&
                    minS > maxS)
                {
                    errors.Add("invalidSelectedRange");
                }
            }
        }
        else if (question.Options.Count > 0)
        {
            errors.Add("optionsNotAllowed");
        }

        if (question.QuestionType == AdmissionQuestionType.Date &&
            question.MinDate is { } minDate &&
            question.MaxDate is { } maxDate &&
            minDate > maxDate)
        {
            errors.Add("invalidDateRange");
        }

        if (question.QuestionType == AdmissionQuestionType.File)
        {
            var extensions = AdmissionRequirementCatalog.ParseExtensions(question.AllowedFileExtensions);
            if (extensions.Count == 0)
            {
                errors.Add("invalidFileTypes");
            }

            if (question.MaxFileSizeBytes is { } size && size <= 0)
            {
                errors.Add("invalidMaxSize");
            }
        }

        return errors;
    }
}

public static class AdmissionQuestionReasonCodes
{
    public const string MissingAnswer = "admission.question.missingAnswer";
    public const string InvalidText = "admission.question.invalidText";
    public const string InvalidChoice = "admission.question.invalidChoice";
    public const string InvalidMultiChoice = "admission.question.invalidMultiChoice";
    public const string InvalidDate = "admission.question.invalidDate";
    public const string InvalidYesNo = "admission.question.invalidYesNo";
    public const string MissingFile = "admission.question.missingFile";
    public const string InvalidFile = "admission.question.invalidFile";
}
