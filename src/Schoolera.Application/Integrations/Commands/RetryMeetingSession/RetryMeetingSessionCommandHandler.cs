using MediatR;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Meetings;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Microsoft.Extensions.Logging;

namespace Schoolera.Application.Integrations.Commands.RetryMeetingSession;

public sealed record RetryMeetingSessionCommand(
    Guid MeetingSessionId, RetryMeetingSessionRequest Body)
    : IRequest<Result<bool>>;

public sealed class RetryMeetingSessionCommandHandler(
    ICurrentUser currentUser,
    IMeetingSessionService meetingSessions,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AdmissionMessages> localizer,
    ILogger<RetryMeetingSessionCommandHandler> logger)
    : IRequestHandler<RetryMeetingSessionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RetryMeetingSessionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorUserId ||
            !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
            return Result<bool>.Failure(
                [localizer["MeetingRetryRejected"]], [AdmissionErrorCodes.MeetingRetryRejected]);
        if (string.IsNullOrWhiteSpace(request.Body.IdempotencyKey) ||
            request.Body.IdempotencyKey.Length > 128 ||
            request.Body.RowVersion.Length == 0)
            return Result<bool>.Failure(
                [localizer["MeetingRetryRejected"]], [AdmissionErrorCodes.MeetingRetryRejected]);
        var retried = await meetingSessions.RetryAsync(
            request.MeetingSessionId, request.Body.RowVersion,
            request.Body.IdempotencyKey, actorUserId, cancellationToken);
        if (retried)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Platform meeting retry accepted for session {MeetingSessionId}.",
                request.MeetingSessionId);
        }
        return retried
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(
                [localizer["MeetingRetryRejected"]], [AdmissionErrorCodes.MeetingRetryRejected]);
    }
}
