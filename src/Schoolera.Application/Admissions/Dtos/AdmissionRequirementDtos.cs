using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Dtos;

public sealed record AdmissionRequirementChecklistItemDto(
    Guid SnapshotId,
    string RequirementCode,
    AdmissionRequirementKind Kind,
    string Name,
    string? Description,
    bool IsRequired,
    int SortOrder,
    bool IsComplete,
    string? ReasonCode,
    string WizardSection,
    AdmissionProfileFieldCode? ProfileFieldCode,
    AdmissionRequiredDocumentCode? DocumentCode,
    IReadOnlyList<string> AllowedFileExtensions,
    long? MaxFileSizeBytes,
    bool AllowChildVaultCopy,
    Guid? LinkedAttachmentId);

public sealed record MissingAdmissionRequirementDto(
    Guid RequirementSnapshotId,
    string RequirementCode,
    AdmissionRequirementKind Kind,
    string DisplayName,
    string ReasonCode,
    string WizardSection,
    AdmissionRequiredDocumentCode? DocumentCode);

public sealed record AdmissionRequirementsIncompleteDto(
    IReadOnlyList<MissingAdmissionRequirementDto> MissingRequirements);

public sealed record SubmitAdmissionOutcomeDto(
    AdmissionApplicationDetailDto? Application,
    IReadOnlyList<MissingAdmissionRequirementDto> MissingRequirements,
    IReadOnlyList<MissingAdmissionQuestionDto> MissingQuestions);

public sealed record PublicAdmissionRequirementSummaryDto(
    string RequirementCode,
    AdmissionRequirementKind Kind,
    string Name,
    string? Description,
    bool IsRequired,
    int SortOrder,
    AdmissionRequiredDocumentCode? DocumentCode,
    IReadOnlyList<string> AllowedFileExtensions,
    long? MaxFileSizeBytes,
    bool AllowChildVaultCopy);
