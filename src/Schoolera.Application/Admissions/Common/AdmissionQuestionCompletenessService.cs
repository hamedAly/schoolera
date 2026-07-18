using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed class AdmissionQuestionCompletenessService(
    ISchoolAdmissionQuestionRepository questionRepository,
    IAdmissionApplicationRepository admissionRepository)
    : IAdmissionQuestionCompletenessService
{
    public async Task<AdmissionQuestionsEvaluation> EvaluateAsync(
        AdmissionApplication application,
        string culture,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<MissingAdmissionQuestionItem>();
        var isArabic = culture.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        var snapshots = (application.QuestionSnapshots?.Count ?? 0) > 0
            ? application.QuestionSnapshots!.ToList()
            : (await questionRepository.ListApplicationSnapshotsAsync(
                application.Id,
                cancellationToken)).ToList();

        var answers = (application.Answers?.Count ?? 0) > 0
            ? application.Answers!.ToList()
            : await LoadAnswersAsync(application, cancellationToken);

        var attachments = (application.Attachments?.Count ?? 0) > 0
            ? application.Attachments!.ToList()
            : await LoadAttachmentsAsync(application, cancellationToken);

        foreach (var snapshot in snapshots.OrderBy(item => item.SortOrder))
        {
            if (!snapshot.IsRequired)
            {
                continue;
            }

            var answer = answers.FirstOrDefault(item => item.QuestionSnapshotId == snapshot.Id);
            var reason = EvaluateSnapshot(snapshot, answer, attachments);
            if (reason is not null)
            {
                missing.Add(CreateMissing(snapshot, isArabic, reason));
            }
        }

        return new AdmissionQuestionsEvaluation(missing.Count == 0, missing);
    }

    private static string? EvaluateSnapshot(
        AdmissionApplicationQuestionSnapshot snapshot,
        AdmissionApplicationAnswer? answer,
        IReadOnlyList<AdmissionApplicationAttachment> attachments)
    {
        return snapshot.QuestionType switch
        {
            AdmissionQuestionType.ShortText or AdmissionQuestionType.LongText =>
                EvaluateText(snapshot, answer?.TextValue),

            AdmissionQuestionType.SingleChoice =>
                EvaluateSingleChoice(snapshot, answer),

            AdmissionQuestionType.MultipleChoice =>
                EvaluateMultipleChoice(snapshot, answer),

            AdmissionQuestionType.Date =>
                EvaluateDate(snapshot, answer?.DateValue),

            AdmissionQuestionType.YesNo =>
                answer?.BooleanValue is null
                    ? AdmissionQuestionReasonCodes.MissingAnswer
                    : null,

            AdmissionQuestionType.File =>
                EvaluateFile(snapshot, answer, attachments),

            _ => AdmissionQuestionReasonCodes.MissingAnswer,
        };
    }

    private static string? EvaluateText(
        AdmissionApplicationQuestionSnapshot snapshot,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return AdmissionQuestionReasonCodes.MissingAnswer;
        }

        var length = text.Trim().Length;
        if (snapshot.MinLength is { } min && length < min)
        {
            return AdmissionQuestionReasonCodes.InvalidText;
        }

        var maxCap = AdmissionQuestionCatalog.DefaultMaxLength(snapshot.QuestionType);
        var max = snapshot.MaxLength ?? maxCap;
        if (length > max)
        {
            return AdmissionQuestionReasonCodes.InvalidText;
        }

        return null;
    }

    private static string? EvaluateSingleChoice(
        AdmissionApplicationQuestionSnapshot snapshot,
        AdmissionApplicationAnswer? answer)
    {
        var selected = answer?.GetSelectedOptionCodes() ?? [];
        if (selected.Count == 0)
        {
            return AdmissionQuestionReasonCodes.MissingAnswer;
        }

        var validCodes = snapshot.Options.Select(option => option.OptionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return validCodes.Contains(selected[0])
            ? null
            : AdmissionQuestionReasonCodes.InvalidChoice;
    }

    private static string? EvaluateMultipleChoice(
        AdmissionApplicationQuestionSnapshot snapshot,
        AdmissionApplicationAnswer? answer)
    {
        var selected = answer?.GetSelectedOptionCodes() ?? [];
        if (selected.Count == 0)
        {
            return AdmissionQuestionReasonCodes.MissingAnswer;
        }

        var validCodes = snapshot.Options.Select(option => option.OptionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selected.Any(code => !validCodes.Contains(code)))
        {
            return AdmissionQuestionReasonCodes.InvalidMultiChoice;
        }

        if (snapshot.MinSelectedOptions is { } min && selected.Count < min)
        {
            return AdmissionQuestionReasonCodes.InvalidMultiChoice;
        }

        if (snapshot.MaxSelectedOptions is { } max && selected.Count > max)
        {
            return AdmissionQuestionReasonCodes.InvalidMultiChoice;
        }

        return null;
    }

    private static string? EvaluateDate(
        AdmissionApplicationQuestionSnapshot snapshot,
        DateOnly? date)
    {
        if (date is null)
        {
            return AdmissionQuestionReasonCodes.MissingAnswer;
        }

        if (snapshot.MinDate is { } min && date < min)
        {
            return AdmissionQuestionReasonCodes.InvalidDate;
        }

        if (snapshot.MaxDate is { } max && date > max)
        {
            return AdmissionQuestionReasonCodes.InvalidDate;
        }

        return null;
    }

    private static string? EvaluateFile(
        AdmissionApplicationQuestionSnapshot snapshot,
        AdmissionApplicationAnswer? answer,
        IReadOnlyList<AdmissionApplicationAttachment> attachments)
    {
        var attachmentId = answer?.AttachmentId;
        var attachment = attachmentId is { } id
            ? attachments.FirstOrDefault(item => item.Id == id)
            : attachments.FirstOrDefault(item => item.QuestionSnapshotId == snapshot.Id);

        if (attachment is null)
        {
            return AdmissionQuestionReasonCodes.MissingFile;
        }

        var allowed = AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions);
        var extension = Path.GetExtension(attachment.OriginalFileName);
        if (allowed.Count > 0 &&
            !allowed.Contains(AdmissionRequirementCatalog.NormalizeExtension(extension)))
        {
            return AdmissionQuestionReasonCodes.InvalidFile;
        }

        if (snapshot.MaxFileSizeBytes is { } max && attachment.FileSizeBytes > max)
        {
            return AdmissionQuestionReasonCodes.InvalidFile;
        }

        return null;
    }

    private async Task<IReadOnlyList<AdmissionApplicationAnswer>> LoadAnswersAsync(
        AdmissionApplication application,
        CancellationToken cancellationToken)
    {
        var loaded = await admissionRepository.GetOwnedAsync(
            application.ParentUserId,
            application.Id,
            cancellationToken);
        return loaded?.Answers?.ToList() ?? [];
    }

    private async Task<IReadOnlyList<AdmissionApplicationAttachment>> LoadAttachmentsAsync(
        AdmissionApplication application,
        CancellationToken cancellationToken)
    {
        var loaded = await admissionRepository.GetOwnedAsync(
            application.ParentUserId,
            application.Id,
            cancellationToken);
        return loaded?.Attachments?.ToList() ?? [];
    }

    private static MissingAdmissionQuestionItem CreateMissing(
        AdmissionApplicationQuestionSnapshot snapshot,
        bool isArabic,
        string reasonCode) =>
        new(
            snapshot.Id,
            snapshot.QuestionCode,
            snapshot.QuestionType,
            isArabic ? snapshot.LabelAr : snapshot.LabelEn,
            reasonCode,
            AdmissionQuestionCatalog.WizardSection);
}
