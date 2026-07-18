using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Common;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Integrations.Dtos;

namespace Schoolera.Application.Integrations.Queries.GetIntegration;

public sealed record GetIntegrationQuery(Guid Id)
    : IRequest<Result<PlatformIntegrationDetailDto>>;

public sealed class GetIntegrationQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetIntegrationQuery, Result<PlatformIntegrationDetailDto>>
{
    public async Task<Result<PlatformIntegrationDetailDto>> Handle(
        GetIntegrationQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Forbidden."],
                [IntegrationErrorCodes.Forbidden]);
        }

        var entity = await notificationRepository.GetIntegrationAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result<PlatformIntegrationDetailDto>.Failure(
                ["Integration not found."],
                [IntegrationErrorCodes.NotFound]);
        }

        return Result<PlatformIntegrationDetailDto>.Success(IntegrationMapping.ToDetail(entity));
    }
}
