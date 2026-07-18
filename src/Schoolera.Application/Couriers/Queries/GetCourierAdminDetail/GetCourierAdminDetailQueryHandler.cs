using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Couriers.Queries.GetCourierAdminDetail;

public sealed record GetCourierAdminDetailQuery(Guid IntegrationId) : IRequest<Result<CourierAdminDetailDto>>;

public sealed class GetCourierAdminDetailQueryHandler(
    ICurrentUser currentUser,
    ICourierAdministrationService service)
    : IRequestHandler<GetCourierAdminDetailQuery, Result<CourierAdminDetailDto>>
{
    public Task<Result<CourierAdminDetailDto>> Handle(GetCourierAdminDetailQuery r, CancellationToken ct) =>
        currentUser.UserId.HasValue && currentUser.IsInRole(SchooleraRoles.PlatformAdmin)
            ? service.GetAsync(r.IntegrationId, ct)
            : Task.FromResult(Result<CourierAdminDetailDto>.Failure(["Forbidden."], [CourierErrorCodes.Forbidden]));
}
