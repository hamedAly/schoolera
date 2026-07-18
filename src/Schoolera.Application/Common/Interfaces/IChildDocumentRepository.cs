using Schoolera.Domain.Entities;

namespace Schoolera.Application.Common.Interfaces;

public interface IChildDocumentRepository
{
    Task<IReadOnlyList<ChildDocument>> ListByChildAsync(
        Guid parentUserId,
        Guid childId,
        CancellationToken cancellationToken = default);

    Task<ChildDocument?> GetOwnedAsync(
        Guid parentUserId,
        Guid childId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<ChildDocument?> GetOwnedForUpdateAsync(
        Guid parentUserId,
        Guid childId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ChildDocument document, CancellationToken cancellationToken = default);

    void Remove(ChildDocument document);

    Task<bool> ExistsSourceOnApplicationAsync(
        Guid applicationId,
        Guid vaultDocumentId,
        CancellationToken cancellationToken = default);

    Task<AdmissionApplicationAttachment?> GetSourceLinkedAttachmentAsync(
        Guid applicationId,
        Guid vaultDocumentId,
        CancellationToken cancellationToken = default);
}
