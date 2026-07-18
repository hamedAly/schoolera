using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Dtos;

public sealed record OnboardingDocumentTypeDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    bool IsRequired,
    int SortOrder)
{
    public static OnboardingDocumentTypeDto FromEntity(SchoolOnboardingDocumentType type) =>
        new(type.Id, type.Code, type.NameAr, type.NameEn, type.IsRequired, type.SortOrder);
}

/// <summary>
/// Active document types plus the upload constraints the Documents step must enforce.
/// </summary>
public sealed record OnboardingDocumentTypesResponseDto(
    IReadOnlyList<OnboardingDocumentTypeDto> DocumentTypes,
    long MaxFileSizeBytes,
    IReadOnlyList<string> AllowedExtensions);
