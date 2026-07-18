using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Common;

internal static class CmsResults
{
    public static bool IsConcurrencyViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().FullName ?? current.GetType().Name;
            if (typeName.Contains("DbUpdateConcurrencyException", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasRowVersionMismatch(byte[]? requested, byte[] current) =>
        requested is { Length: > 0 } &&
        current.Length > 0 &&
        !requested.AsSpan().SequenceEqual(current);

    public static Result<T> ConcurrencyFailure<T>() =>
        Result<T>.Failure(
            ["The content was updated by another user."],
            [CmsErrorCodes.PageConcurrentUpdate]);

    public static async Task<Result<T>?> TrySaveAsync<T>(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (Exception ex) when (IsConcurrencyViolation(ex))
        {
            return ConcurrencyFailure<T>();
        }
    }
}

internal static class CmsPublishRules
{
    public static bool HasPublishableBilingualContent(
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn) =>
        !string.IsNullOrWhiteSpace(titleAr) &&
        !string.IsNullOrWhiteSpace(titleEn) &&
        !string.IsNullOrWhiteSpace(contentAr) &&
        !string.IsNullOrWhiteSpace(contentEn);
}
