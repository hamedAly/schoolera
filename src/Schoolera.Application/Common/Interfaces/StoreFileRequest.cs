namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// File payload for storage. Avoids coupling Application to ASP.NET Core <c>IFormFile</c>.
/// </summary>
public sealed class StoreFileRequest
{
    public required Stream Content { get; init; }

    public required string OriginalFileName { get; init; }

    public required string ContentType { get; init; }

    /// <summary>
    /// Logical folder/category under the public uploads root, for example <c>schools/logos</c>.
    /// </summary>
    public required string Category { get; init; }
}
