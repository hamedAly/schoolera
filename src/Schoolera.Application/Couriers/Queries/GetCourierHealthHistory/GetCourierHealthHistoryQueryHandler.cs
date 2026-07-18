using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Couriers.Queries.GetCourierHealthHistory;

public sealed record GetCourierHealthHistoryQuery(Guid IntegrationId, int Take)
    : IRequest<Result<IReadOnlyList<CourierHealthDto>>>;

public sealed class GetCourierHealthHistoryQueryHandler(
    ICurrentUser currentUser,
    ICourierAdministrationService service)
    : IRequestHandler<GetCourierHealthHistoryQuery, Result<IReadOnlyList<CourierHealthDto>>>
{
    public Task<Result<IReadOnlyList<CourierHealthDto>>> Handle(
        GetCourierHealthHistoryQuery r, CancellationToken ct) =>
        currentUser.UserId.HasValue && currentUser.IsInRole(SchooleraRoles.PlatformAdmin)
            ? service.HealthHistoryAsync(r.IntegrationId, r.Take, ct)
            : Task.FromResult(Result<IReadOnlyList<CourierHealthDto>>.Failure(
                ["Forbidden."], [CourierErrorCodes.Forbidden]));
}
