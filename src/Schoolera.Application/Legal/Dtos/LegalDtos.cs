using Schoolera.Domain.Enums;

namespace Schoolera.Application.Legal.Dtos;

public sealed record CurrentLegalDocumentsDto(
    CurrentLegalDocumentDto Terms,
    CurrentLegalDocumentDto Privacy);

public sealed record CurrentLegalDocumentDto(
    LegalDocumentType DocumentType,
    string Path,
    int VersionNumber,
    string Title,
    DateTimeOffset PublishedAtUtc);
