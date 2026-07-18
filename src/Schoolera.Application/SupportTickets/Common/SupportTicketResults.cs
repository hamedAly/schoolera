using Schoolera.Application.Common.Exceptions;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Constants;

namespace Schoolera.Application.SupportTickets.Common;

internal static class SupportTicketResults
{
    public static Result<T> Forbidden<T>() =>
        Result<T>.Failure(["Forbidden."], [SupportTicketErrorCodes.Forbidden]);

    public static Result<T> NotFound<T>() =>
        Failure<T>("Support ticket not found.", SupportTicketErrorCodes.NotFound);

    public static Result<T> Failure<T>(string message, string errorCode) =>
        Result<T>.Failure([message], [errorCode]);

    public static async Task<Result<T>?> TrySaveAsync<T>(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConcurrencyConflictException)
        {
            return Failure<T>("The ticket was updated by another user.", SupportTicketErrorCodes.ConcurrentUpdate);
        }
    }
}
