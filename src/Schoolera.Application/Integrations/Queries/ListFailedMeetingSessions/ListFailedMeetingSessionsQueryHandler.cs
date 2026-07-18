using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Meetings;

namespace Schoolera.Application.Integrations.Queries.ListFailedMeetingSessions;

public sealed record ListFailedMeetingSessionsQuery(int Take = 50)
    : IRequest<Result<IReadOnlyList<FailedMeetingSessionListItemDto>>>;

public sealed class ListFailedMeetingSessionsQueryHandler(
    ICurrentUser currentUser,
    IMeetingSessionService meetingSessions)
    : IRequestHandler<ListFailedMeetingSessionsQuery,
        Result<IReadOnlyList<FailedMeetingSessionListItemDto>>>
{
    public async Task<Result<IReadOnlyList<FailedMeetingSessionListItemDto>>> Handle(
        ListFailedMeetingSessionsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
            return Result<IReadOnlyList<FailedMeetingSessionListItemDto>>.Failure(
                ["Forbidden."], [IntegrationErrorCodes.Forbidden]);
        return Result<IReadOnlyList<FailedMeetingSessionListItemDto>>.Success(
            await meetingSessions.ListFailedAsync(request.Take, cancellationToken));
    }
}
