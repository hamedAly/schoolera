using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Common;

internal static class ChildDocumentFileRules
{
    private static readonly HashSet<string> PhotoExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly HashSet<string> PhotoContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
        };

    public static string? ValidateBeforeSave(
        ChildDocumentType documentType,
        string originalFileName,
        string contentType,
        long fileSize,
        PrivateFileStoragePolicy policy)
    {
        if (!Enum.IsDefined(documentType))
        {
            return ParentErrorCodes.DocumentInvalidType;
        }

        if (fileSize <= 0)
        {
            return ParentErrorCodes.DocumentInvalidFile;
        }

        if (fileSize > policy.MaxFileSizeBytes)
        {
            return ParentErrorCodes.DocumentTooLarge;
        }

        var extension = Path.GetExtension(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ParentErrorCodes.DocumentInvalidFile;
        }

        if (!extension.StartsWith(".", StringComparison.Ordinal))
        {
            extension = $".{extension}";
        }

        extension = extension.ToLowerInvariant();
        var normalizedContentType = (contentType ?? string.Empty).Trim().ToLowerInvariant();

        if (documentType == ChildDocumentType.ChildPhoto)
        {
            if (!PhotoExtensions.Contains(extension) || !PhotoContentTypes.Contains(normalizedContentType))
            {
                return ParentErrorCodes.DocumentInvalidFile;
            }

            return null;
        }

        if (!policy.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
            !policy.AllowedContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
        {
            return ParentErrorCodes.DocumentInvalidFile;
        }

        return null;
    }

    public static string MapPrivateFileError(PrivateFileValidationException exception) =>
        exception.ErrorCode switch
        {
            OnboardingErrorCodes.DocumentTooLarge => ParentErrorCodes.DocumentTooLarge,
            _ => ParentErrorCodes.DocumentInvalidFile,
        };

    public static string MessageFor(string errorCode) =>
        errorCode switch
        {
            ParentErrorCodes.DocumentInvalidType => "Document type is invalid.",
            ParentErrorCodes.DocumentTooLarge => "Document exceeds the maximum allowed size.",
            ParentErrorCodes.DocumentCopyFailed => "Document could not be copied to the application.",
            ParentErrorCodes.DocumentAlreadyAttached => "Document is already attached to this application.",
            _ => "Document type or format is invalid.",
        };
}
