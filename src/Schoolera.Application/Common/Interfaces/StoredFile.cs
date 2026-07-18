namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// Result of a successful store operation. <see cref="RelativePublicUrl"/> is safe to persist in the database.
/// </summary>
public sealed record StoredFile(
    string RelativePublicUrl,
    string StoredFileName,
    long SizeBytes,
    string ContentType);
