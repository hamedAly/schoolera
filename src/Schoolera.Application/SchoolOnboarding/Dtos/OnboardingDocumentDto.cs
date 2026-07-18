using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Dtos;

/// <summary>Owner/admin-visible document metadata. Never exposes storage references or paths.</summary>
public sealed record OnboardingDocumentDto(
    Guid Id,
    Guid DocumentTypeId,
    string DocumentTypeCode,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAtUtc)
{
    public static OnboardingDocumentDto FromEntity(SchoolOnboardingDocument document) =>
        new(
            document.Id,
            document.DocumentTypeId,
            document.DocumentType?.Code ?? string.Empty,
            document.OriginalFileName,
            document.ContentType,
            document.FileSize,
            document.UploadedAtUtc);
}
