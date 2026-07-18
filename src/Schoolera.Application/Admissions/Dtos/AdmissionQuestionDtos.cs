using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Dtos;

public sealed record AdmissionQuestionOptionSnapshotDto(
    string OptionCode,
    string Label,
    int SortOrder);

public sealed record AdmissionQuestionChecklistItemDto(
    Guid SnapshotId,
    string QuestionCode,
    AdmissionQuestionType QuestionType,
    string Label,
    string? Help,
    bool IsRequired,
    int SortOrder,
    bool IsComplete,
    string? ReasonCode,
    string WizardSection,
    int? MinLength,
    int? MaxLength,
    int? MinSelectedOptions,
    int? MaxSelectedOptions,
    DateOnly? MinDate,
    DateOnly? MaxDate,
    IReadOnlyList<string> AllowedFileExtensions,
    long? MaxFileSizeBytes,
    bool AllowChildVaultCopy,
    IReadOnlyList<AdmissionQuestionOptionSnapshotDto> Options,
    string? TextValue,
    IReadOnlyList<string> SelectedOptionCodes,
    DateOnly? DateValue,
    bool? BooleanValue,
    Guid? LinkedAttachmentId);

public sealed record MissingAdmissionQuestionDto(
    Guid QuestionSnapshotId,
    string QuestionCode,
    AdmissionQuestionType QuestionType,
    string DisplayName,
    string ReasonCode,
    string WizardSection);

public sealed record AdmissionQuestionsIncompleteDto(
    IReadOnlyList<MissingAdmissionQuestionDto> MissingQuestions);

public sealed record UpsertAdmissionAnswerRequest(
    Guid QuestionSnapshotId,
    string? TextValue,
    IReadOnlyList<string>? SelectedOptionCodes,
    DateOnly? DateValue,
    bool? BooleanValue,
    Guid? AttachmentId);

public sealed record AdmissionApplicationQuestionsDto(
    Guid ApplicationId,
    IReadOnlyList<AdmissionQuestionChecklistItemDto> Questions);
