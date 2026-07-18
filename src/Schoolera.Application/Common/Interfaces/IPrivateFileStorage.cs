namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// Private, authenticated-only file storage for sensitive onboarding documents.
/// Files stored here are never served by static-file middleware and never have public URLs.
/// </summary>
public interface IPrivateFileStorage
{
    PrivateFileStoragePolicy Policy { get; }

    Task<StoredPrivateFile> SaveAsync(
        PrivateFileStoreRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a read stream for a stored reference, or null when missing.</summary>
    Task<Stream?> OpenReadAsync(
        string storedFileReference,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storedFileReference,
        CancellationToken cancellationToken = default);
}

public sealed record PrivateFileStoragePolicy(
    long MaxFileSizeBytes,
    IReadOnlyList<string> AllowedExtensions,
    IReadOnlyList<string> AllowedContentTypes);

public sealed class PrivateFileStoreRequest
{
    public required Stream Content { get; init; }

    public required string OriginalFileName { get; init; }

    public required string ContentType { get; init; }

    public long? DeclaredSizeBytes { get; init; }

    /// <summary>Logical sub-folder (sanitized), typically the application id.</summary>
    public required string Category { get; init; }
}

public sealed record StoredPrivateFile(
    string StoredFileReference,
    string StoredFileName,
    long SizeBytes,
    string ContentType,
    string Sha256Hash);

/// <summary>
/// Thrown by private storage when an uploaded file fails a safety check. The
/// <see cref="ErrorCode"/> is a stable onboarding error code for client branching.
/// </summary>
public sealed class PrivateFileValidationException(string errorCode, string message)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
