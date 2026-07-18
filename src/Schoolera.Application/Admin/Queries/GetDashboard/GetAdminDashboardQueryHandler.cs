using MediatR;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Queries.GetDashboard;

public sealed record GetAdminDashboardQuery : IRequest<Result<AdminDashboardDto>>;

public sealed class GetAdminDashboardQueryHandler(IAdminPlatformService adminPlatform)
    : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDto>>
{
    public async Task<Result<AdminDashboardDto>> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var dashboard = await adminPlatform.GetDashboardAsync(cancellationToken);
        return Result<AdminDashboardDto>.Success(dashboard);
    }
}
