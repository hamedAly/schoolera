namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// Application-layer file storage contract. Implementations must never expose physical server paths.
/// </summary>
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(StoreFileRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePublicUrl, CancellationToken cancellationToken = default);
}
